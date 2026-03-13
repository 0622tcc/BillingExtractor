using BillingExtractor.Api.Models;

namespace BillingExtractor.Api.Services.Rules;

/// <summary>
/// Verifies that the sum of line item totals matches the invoice's stated total amount.
/// Uses computed properties on ExtractedInvoice (encapsulation).
/// </summary>
public class AmountMismatchValidationRule : IInvoiceValidationRule
{
    public Task ApplyAsync(ExtractedInvoice invoice, ValidationResult result)
    {
        result.CalculatedTotal = invoice.CalculatedTotal;

        if (invoice.HasAmountMismatch)
        {
            result.HasAmountMismatch = true;
            result.Warnings.Add(
                $"Amount mismatch: line items sum to {invoice.CalculatedTotal:C} but invoice states {invoice.TotalAmount:C}");
        }

        return Task.CompletedTask;
    }
}
