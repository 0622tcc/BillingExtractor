using BillingExtractor.Api.Models;

namespace BillingExtractor.Api.Services.Rules;

/// <summary>
/// A single composable validation rule applied to an extracted invoice (OCP).
/// Add new rules by implementing this interface — no existing code changes required.
/// </summary>
public interface IInvoiceValidationRule
{
    Task ApplyAsync(ExtractedInvoice invoice, ValidationResult result);
}
