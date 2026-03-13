using BillingExtractor.Api.Data.Entities;

namespace BillingExtractor.Api.Services.Interfaces;

/// <summary>Read-only operations against the invoice store (ISP: consumers that only read).</summary>
public interface IInvoiceReadRepository
{
    Task<InvoiceRecord?> FindByInvoiceNumberAsync(string invoiceNumber);
    Task<IEnumerable<InvoiceRecord>> GetAllAsync();
}
