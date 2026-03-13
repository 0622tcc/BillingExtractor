namespace BillingExtractor.Api.Models;

/// <summary>
/// Application-layer result returned by IInvoiceService.
/// Lives in Models (not DTOs) so the service layer has no dependency on HTTP/presentation concerns.
/// The controller maps this to ExtractResponse (the HTTP DTO) before returning.
/// </summary>
public record InvoiceProcessingResult(
    ExtractedInvoice Invoice,
    ValidationResult Validation,
    bool Saved);
