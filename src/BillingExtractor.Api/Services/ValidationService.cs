using BillingExtractor.Api.Models;
using BillingExtractor.Api.Services.Interfaces;
using BillingExtractor.Api.Services.Rules;

namespace BillingExtractor.Api.Services;

/// <summary>
/// Runs all registered IInvoiceValidationRule implementations against an extracted invoice (OCP).
/// New validation rules can be added by registering additional IInvoiceValidationRule implementations
/// in the DI container — no changes to this class required.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly IEnumerable<IInvoiceValidationRule> _rules;

    public ValidationService(IEnumerable<IInvoiceValidationRule> rules)
        => _rules = rules;

    public async Task<ValidationResult> ValidateAsync(ExtractedInvoice invoice)
    {
        var result = new ValidationResult { StatedTotal = invoice.TotalAmount };

        foreach (var rule in _rules)
            await rule.ApplyAsync(invoice, result);

        return result;
    }
}
