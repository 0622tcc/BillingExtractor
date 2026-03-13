using BillingExtractor.Api.Controllers;
using BillingExtractor.Api.Data.Entities;
using BillingExtractor.Api.Models;
using BillingExtractor.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BillingExtractor.Tests.Controllers;

public class InvoicesControllerTests
{
    private readonly Mock<IInvoiceService> _serviceMock = new();
    private readonly Mock<IInvoiceReadRepository> _readRepoMock = new();
    private readonly Mock<IInvoiceWriteRepository> _writeRepoMock = new();
    private readonly InvoicesController _sut;

    public InvoicesControllerTests()
    {
        _sut = new InvoicesController(
            _serviceMock.Object,
            _readRepoMock.Object,
            _writeRepoMock.Object,
            NullLogger<InvoicesController>.Instance);
    }

    private static IFormFile MakeFormFile(string fileName, string contentType, int sizeBytes = 1024)
    {
        var content = new byte[sizeBytes];
        var stream = new MemoryStream(content);
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(sizeBytes);
        mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, CancellationToken>((s, _) => stream.CopyTo(s))
            .Returns(Task.CompletedTask);
        return mock.Object;
    }

    // --- POST /api/invoices/extract ---

    [Fact]
    public async Task Extract_ValidFile_Returns200WithExtractResponse()
    {
        var file = MakeFormFile("invoice.png", "image/png");
        var invoice = new ExtractedInvoice { InvoiceNumber = "INV-001", TotalAmount = 100m };
        var validation = new ValidationResult { StatedTotal = 100m, CalculatedTotal = 100m };
        var processingResult = new InvoiceProcessingResult(invoice, validation, Saved: true);

        _serviceMock
            .Setup(s => s.ProcessAsync(It.IsAny<byte[]>(), "image/png"))
            .ReturnsAsync(processingResult);

        var result = await _sut.Extract(file);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        _serviceMock.Verify(s => s.ProcessAsync(It.IsAny<byte[]>(), "image/png"), Times.Once);
    }

    [Fact]
    public async Task Extract_InvalidFileType_Returns400()
    {
        var file = MakeFormFile("document.pdf", "application/pdf");

        var result = await _sut.Extract(file);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, bad.StatusCode);
        _serviceMock.Verify(s => s.ProcessAsync(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Extract_FileTooLarge_Returns400()
    {
        var file = MakeFormFile("big.png", "image/png", sizeBytes: 11 * 1024 * 1024);

        var result = await _sut.Extract(file);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, bad.StatusCode);
    }

    [Fact]
    public async Task Extract_NullFile_Returns400()
    {
        var result = await _sut.Extract(null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Extract_ServiceThrows_Returns500()
    {
        var file = MakeFormFile("invoice.png", "image/png");
        _serviceMock
            .Setup(s => s.ProcessAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("AI unavailable"));

        var result = await _sut.Extract(file);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, serverError.StatusCode);
    }

    // --- GET /api/invoices ---

    [Fact]
    public async Task GetAll_ReturnsListFromReadRepository()
    {
        var records = new List<InvoiceRecord>
        {
            new() { Id = 1, InvoiceNumber = "INV-001", VendorName = "Acme", Date = "2024-01-01", TotalAmount = 100m, LineItemsJson = "[]" },
            new() { Id = 2, InvoiceNumber = "INV-002", VendorName = "Globe", Date = "2024-01-02", TotalAmount = 200m, LineItemsJson = "[]" }
        };
        _readRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(records);

        var result = await _sut.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    // --- DELETE /api/invoices/{id} ---

    [Fact]
    public async Task Delete_ExistingId_Returns204()
    {
        _writeRepoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _sut.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        _writeRepoMock.Setup(r => r.DeleteAsync(999)).ReturnsAsync(false);

        var result = await _sut.Delete(999);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }
}
