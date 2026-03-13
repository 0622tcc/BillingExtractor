using BillingExtractor.Api.Models;
using BillingExtractor.Api.Services.Rules;
using Xunit;

namespace BillingExtractor.Tests.Rules;

public class AmountMismatchValidationRuleTests
{
    private readonly AmountMismatchValidationRule _sut = new();

    private static ExtractedInvoice MakeInvoice(decimal stated, decimal lineTotal) => new()
    {
        TotalAmount = stated,
        LineItems = new List<LineItem>
        {
            new() { Description = "Item", Quantity = 1, UnitPrice = lineTotal, LineTotal = lineTotal }
        }
    };

    [Fact]
    public async Task ApplyAsync_MatchingTotals_NoMismatch()
    {
        var invoice = MakeInvoice(stated: 100m, lineTotal: 100m);
        var result = new ValidationResult();

        await _sut.ApplyAsync(invoice, result);

        Assert.False(result.HasAmountMismatch);
        Assert.Equal(100m, result.CalculatedTotal);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task ApplyAsync_MismatchedTotals_SetsFlagAndAddsWarning()
    {
        var invoice = MakeInvoice(stated: 200m, lineTotal: 100m);
        var result = new ValidationResult();

        await _sut.ApplyAsync(invoice, result);

        Assert.True(result.HasAmountMismatch);
        Assert.Equal(100m, result.CalculatedTotal);
        Assert.Single(result.Warnings);
        Assert.Contains("mismatch", result.Warnings[0]);
    }

    [Fact]
    public async Task ApplyAsync_WithinTolerance_NotFlaggedAsMismatch()
    {
        // Items sum to 100, stated is 100.005 — within 0.01 tolerance
        var invoice = MakeInvoice(stated: 100.005m, lineTotal: 100m);
        var result = new ValidationResult();

        await _sut.ApplyAsync(invoice, result);

        Assert.False(result.HasAmountMismatch);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task ApplyAsync_AlwaysSetsCalculatedTotal_EvenWhenMatch()
    {
        var invoice = MakeInvoice(stated: 75m, lineTotal: 75m);
        var result = new ValidationResult();

        await _sut.ApplyAsync(invoice, result);

        Assert.Equal(75m, result.CalculatedTotal);
    }

    [Fact]
    public async Task ApplyAsync_EmptyLineItems_CalculatedTotalIsZero()
    {
        var invoice = new ExtractedInvoice { TotalAmount = 50m, LineItems = new List<LineItem>() };
        var result = new ValidationResult();

        await _sut.ApplyAsync(invoice, result);

        Assert.Equal(0m, result.CalculatedTotal);
        Assert.True(result.HasAmountMismatch);
    }
}
