using BillingExtractor.Api.Models;
using BillingExtractor.Api.Services;
using BillingExtractor.Api.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BillingExtractor.Tests.Services;

/// <summary>
/// Tests InvoiceService orchestration:
/// LLM extraction → validation → conditional save.
/// </summary>
public class InvoiceServiceTests
{
    private readonly Mock<ILlmService> _llmMock = new();
    private readonly Mock<IValidationService> _validationMock = new();
    private readonly Mock<IInvoiceWriteRepository> _writeRepoMock = new();
    private readonly InvoiceService _sut;

    public InvoiceServiceTests()
    {
        _sut = new InvoiceService(
            _llmMock.Object,
            _validationMock.Object,
            _writeRepoMock.Object,
            NullLogger<InvoiceService>.Instance);
    }

    [Fact]
    public async Task ProcessAsync_ValidInvoice_ExtractsValidatesSavesAndReturnsSavedTrue()
    {
        var invoice = new ExtractedInvoice { InvoiceNumber = "INV-001", TotalAmount = 100m };
        var validation = new ValidationResult { IsDuplicate = false };
        _llmMock.Setup(l => l.ExtractInvoiceAsync(It.IsAny<byte[]>(), "image/png")).ReturnsAsync(invoice);
        _validationMock.Setup(v => v.ValidateAsync(invoice)).ReturnsAsync(validation);

        var result = await _sut.ProcessAsync(new byte[10], "image/png");

        Assert.Equal(invoice, result.Invoice);
        Assert.Equal(validation, result.Validation);
        Assert.True(result.Saved);
        _writeRepoMock.Verify(r => r.SaveAsync(invoice), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_DuplicateInvoice_SkipsSaveAndReturnsSavedFalse()
    {
        var invoice = new ExtractedInvoice { InvoiceNumber = "INV-DUP" };
        var validation = new ValidationResult { IsDuplicate = true };
        _llmMock.Setup(l => l.ExtractInvoiceAsync(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync(invoice);
        _validationMock.Setup(v => v.ValidateAsync(invoice)).ReturnsAsync(validation);

        var result = await _sut.ProcessAsync(new byte[10], "image/jpeg");

        Assert.False(result.Saved);
        _writeRepoMock.Verify(r => r.SaveAsync(It.IsAny<ExtractedInvoice>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_CallsLlmWithCorrectMimeType()
    {
        var invoice = new ExtractedInvoice();
        var validation = new ValidationResult();
        _llmMock.Setup(l => l.ExtractInvoiceAsync(It.IsAny<byte[]>(), "image/jpeg")).ReturnsAsync(invoice);
        _validationMock.Setup(v => v.ValidateAsync(It.IsAny<ExtractedInvoice>())).ReturnsAsync(validation);

        await _sut.ProcessAsync(new byte[5], "image/jpeg");

        _llmMock.Verify(l => l.ExtractInvoiceAsync(It.IsAny<byte[]>(), "image/jpeg"), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsExtractedInvoiceFromLlm()
    {
        var invoice = new ExtractedInvoice { InvoiceNumber = "INV-999", VendorName = "Corp" };
        var validation = new ValidationResult();
        _llmMock.Setup(l => l.ExtractInvoiceAsync(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync(invoice);
        _validationMock.Setup(v => v.ValidateAsync(invoice)).ReturnsAsync(validation);

        var result = await _sut.ProcessAsync(new byte[1], "image/png");

        Assert.Equal("INV-999", result.Invoice.InvoiceNumber);
        Assert.Equal("Corp", result.Invoice.VendorName);
    }
}
