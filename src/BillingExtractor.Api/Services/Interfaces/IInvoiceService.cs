using BillingExtractor.Api.Models;

namespace BillingExtractor.Api.Services.Interfaces;

/// <summary>
/// Orchestrates the full invoice processing pipeline:
/// extract (LLM) → validate → persist (if not duplicate).
/// Returns InvoiceProcessingResult (an application-layer type in Models/).
/// The controller maps this to an HTTP-specific DTO — keeping service layer
/// free of presentation concerns (SRP / Layered Architecture).
/// </summary>
public interface IInvoiceService
{
    Task<InvoiceProcessingResult> ProcessAsync(byte[] imageBytes, string mimeType);
}
