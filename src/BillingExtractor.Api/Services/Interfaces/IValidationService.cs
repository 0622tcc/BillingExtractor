using BillingExtractor.Api.Models;

namespace BillingExtractor.Api.Services.Interfaces;

public interface IValidationService
{
    Task<ValidationResult> ValidateAsync(ExtractedInvoice invoice);
}
