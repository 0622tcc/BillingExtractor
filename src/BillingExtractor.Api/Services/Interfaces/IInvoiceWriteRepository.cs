using BillingExtractor.Api.Models;

namespace BillingExtractor.Api.Services.Interfaces;

/// <summary>
/// Write operations against the invoice store (ISP: consumers that only write).
/// Accepts the domain model ExtractedInvoice — the repository implementation
/// is responsible for mapping it to the persistence entity internally.
/// Returns Task (not the entity) to avoid leaking the infrastructure InvoiceRecord type
/// through the application-layer interface.
/// </summary>
public interface IInvoiceWriteRepository
{
    Task SaveAsync(ExtractedInvoice invoice);
    Task<bool> DeleteAsync(int id);
}
