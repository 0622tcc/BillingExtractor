using BillingExtractor.Api.DTOs;
using BillingExtractor.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BillingExtractor.Api.Controllers;

/// <summary>
/// HTTP adapter for invoice operations.
/// Handles only HTTP concerns (file validation, routing, status codes).
/// All business logic is delegated to IInvoiceService (SRP / MVC).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IInvoiceReadRepository _readRepository;
    private readonly IInvoiceWriteRepository _writeRepository;
    private readonly ILogger<InvoicesController> _logger;

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/jpg"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    public InvoicesController(
        IInvoiceService invoiceService,
        IInvoiceReadRepository readRepository,
        IInvoiceWriteRepository writeRepository,
        ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Extracts invoice data from an uploaded image using AI.
    /// </summary>
    /// <param name="file">PNG or JPG invoice image, max 10MB.</param>
    /// <returns>Extracted invoice data, validation results, and save status.</returns>
    /// <response code="200">Invoice extracted successfully.</response>
    /// <response code="400">Invalid file type or size.</response>
    /// <response code="500">Extraction or processing error.</response>
    [HttpPost("extract")]
    [ProducesResponseType(typeof(ExtractResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> Extract(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ErrorResponse("No file uploaded."));

        if (!AllowedMimeTypes.Contains(file.ContentType))
            return BadRequest(new ErrorResponse("Only PNG and JPG files are allowed."));

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new ErrorResponse("File size exceeds 10MB limit."));

        try
        {
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            _logger.LogInformation("Received invoice file {FileName} ({Size} bytes)", file.FileName, bytes.Length);

            // Service returns an application-layer result; controller maps it to the HTTP DTO
            var result = await _invoiceService.ProcessAsync(bytes, file.ContentType);
            return Ok(new ExtractResponse(result.Invoice, result.Validation, result.Saved));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invoice file {FileName}", file.FileName);
            return StatusCode(500, new ErrorResponse($"Processing failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Deletes an invoice record by ID. Useful for resetting demo/test data.
    /// </summary>
    /// <param name="id">The invoice record ID.</param>
    /// <response code="204">Deleted successfully.</response>
    /// <response code="404">Invoice not found.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _writeRepository.DeleteAsync(id);
        if (!deleted)
            return NotFound(new ErrorResponse($"Invoice with ID {id} not found."));

        _logger.LogInformation("Invoice record {Id} deleted", id);
        return NoContent();
    }

    /// <summary>
    /// Returns all previously processed invoices.
    /// </summary>
    /// <returns>List of invoice records with deserialized line items.</returns>
    /// <response code="200">List of invoices.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InvoiceRecordDto>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var records = await _readRepository.GetAllAsync();
        var dtos = records.Select(r => new InvoiceRecordDto(r));
        return Ok(dtos);
    }
}
