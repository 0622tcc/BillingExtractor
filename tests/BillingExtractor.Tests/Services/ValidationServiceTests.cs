using BillingExtractor.Api.Models;
using BillingExtractor.Api.Services;
using BillingExtractor.Api.Services.Rules;
using Moq;
using Xunit;

namespace BillingExtractor.Tests.Services;

/// <summary>
/// Tests ValidationService as a rule runner (OCP).
/// Verifies it calls every registered rule and initialises StatedTotal correctly.
/// Logic for individual rules lives in DuplicateValidationRuleTests and
/// AmountMismatchValidationRuleTests.
/// </summary>
public class ValidationServiceTests
{
    [Fact]
    public async Task ValidateAsync_CallsAllRegisteredRules()
    {
        var rule1 = new Mock<IInvoiceValidationRule>();
        var rule2 = new Mock<IInvoiceValidationRule>();
        var invoice = new ExtractedInvoice { InvoiceNumber = "INV-001", TotalAmount = 100m };

        var sut = new ValidationService(new[] { rule1.Object, rule2.Object });
        await sut.ValidateAsync(invoice);

        rule1.Verify(r => r.ApplyAsync(invoice, It.IsAny<ValidationResult>()), Times.Once);
        rule2.Verify(r => r.ApplyAsync(invoice, It.IsAny<ValidationResult>()), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_SetsStatedTotal_FromInvoice()
    {
        var sut = new ValidationService(Enumerable.Empty<IInvoiceValidationRule>());
        var invoice = new ExtractedInvoice { TotalAmount = 123.45m };

        var result = await sut.ValidateAsync(invoice);

        Assert.Equal(123.45m, result.StatedTotal);
    }

    [Fact]
    public async Task ValidateAsync_NoRules_ReturnsCleanResult()
    {
        var sut = new ValidationService(Enumerable.Empty<IInvoiceValidationRule>());
        var invoice = new ExtractedInvoice { TotalAmount = 50m };

        var result = await sut.ValidateAsync(invoice);

        Assert.False(result.IsDuplicate);
        Assert.False(result.HasAmountMismatch);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task ValidateAsync_RuleModifiesResult_ChangeIsVisible()
    {
        var rule = new Mock<IInvoiceValidationRule>();
        rule.Setup(r => r.ApplyAsync(It.IsAny<ExtractedInvoice>(), It.IsAny<ValidationResult>()))
            .Callback<ExtractedInvoice, ValidationResult>((_, res) =>
            {
                res.IsDuplicate = true;
                res.Warnings.Add("duplicate");
            })
            .Returns(Task.CompletedTask);

        var sut = new ValidationService(new[] { rule.Object });
        var result = await sut.ValidateAsync(new ExtractedInvoice());

        Assert.True(result.IsDuplicate);
        Assert.Single(result.Warnings);
    }
}
