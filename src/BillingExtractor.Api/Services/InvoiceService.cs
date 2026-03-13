using BillingExtractor.Api.Models;
using BillingExtractor.Api.Services.Interfaces;

namespace BillingExtractor.Api.Services;

/// <inheritdoc />
public class InvoiceService : IInvoiceService
{
    private readonly ILlmService _llmService;
    private readonly IValidationService _validationService;
    private readonly IInvoiceWriteRepository _writeRepository;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        ILlmService llmService,
        IValidationService validationService,
        IInvoiceWriteRepository writeRepository,
        ILogger<InvoiceService> logger)
    {
        _llmService = llmService;
        _validationService = validationService;
        _writeRepository = writeRepository;
        _logger = logger;
    }

    public async Task<InvoiceProcessingResult> ProcessAsync(byte[] imageBytes, string mimeType)
    {
        _logger.LogInformation("Processing invoice image ({Size} bytes, {MimeType})", imageBytes.Length, mimeType);

        var extracted = await _llmService.ExtractInvoiceAsync(imageBytes, mimeType);
        var validation = await _validationService.ValidateAsync(extracted);

        bool saved = false;
        if (!validation.IsDuplicate)
        {
            await _writeRepository.SaveAsync(extracted);
            saved = true;
            _logger.LogInformation("Invoice #{InvoiceNumber} saved successfully", extracted.InvoiceNumber);
        }
        else
        {
            _logger.LogWarning("Invoice #{InvoiceNumber} skipped — duplicate detected", extracted.InvoiceNumber);
        }

        return new InvoiceProcessingResult(extracted, validation, saved);
    }
}
