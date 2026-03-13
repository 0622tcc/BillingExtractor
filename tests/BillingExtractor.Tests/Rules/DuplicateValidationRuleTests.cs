using BillingExtractor.Api.Data.Entities;
using BillingExtractor.Api.Models;
using BillingExtractor.Api.Services.Interfaces;
using BillingExtractor.Api.Services.Rules;
using Moq;
using Xunit;

namespace BillingExtractor.Tests.Rules;

public class DuplicateValidationRuleTests
{
    private readonly Mock<IInvoiceReadRepository> _repoMock = new();
    private readonly DuplicateValidationRule _sut;

    public DuplicateValidationRuleTests()
        => _sut = new DuplicateValidationRule(_repoMock.Object);

    [Fact]
    public async Task ApplyAsync_NoDuplicate_LeavesResultUnchanged()
    {
        _repoMock.Setup(r => r.FindByInvoiceNumberAsync("INV-001")).ReturnsAsync((InvoiceRecord?)null);
        var invoice = new ExtractedInvoice { InvoiceNumber = "INV-001" };
        var result = new ValidationResult();

        await _sut.ApplyAsync(invoice, result);

        Assert.False(result.IsDuplicate);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task ApplyAsync_DuplicateFound_SetsFlagAndAddsWarning()
    {
        var existing = new InvoiceRecord { InvoiceNumber = "INV-001", Date = "2024-01-10" };
        _repoMock.Setup(r => r.FindByInvoiceNumberAsync("INV-001")).ReturnsAsync(existing);
        var invoice = new ExtractedInvoice { InvoiceNumber = "INV-001" };
        var result = new ValidationResult();

        await _sut.ApplyAsync(invoice, result);

        Assert.True(result.IsDuplicate);
        Assert.Single(result.Warnings);
        Assert.Contains("INV-001", result.Warnings[0]);
        Assert.Contains("already submitted", result.Warnings[0]);
    }

    [Fact]
    public async Task ApplyAsync_DuplicateFound_WarningContainsExistingDate()
    {
        var existing = new InvoiceRecord { InvoiceNumber = "INV-002", Date = "2024-03-15" };
        _repoMock.Setup(r => r.FindByInvoiceNumberAsync("INV-002")).ReturnsAsync(existing);
        var invoice = new ExtractedInvoice { InvoiceNumber = "INV-002" };
        var result = new ValidationResult();

        await _sut.ApplyAsync(invoice, result);

        Assert.Contains("2024-03-15", result.Warnings[0]);
    }
}
