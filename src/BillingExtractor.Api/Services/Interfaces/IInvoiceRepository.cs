namespace BillingExtractor.Api.Services.Interfaces;

/// <summary>
/// Composite repository interface for consumers that need both read and write access.
/// Prefer the more specific IInvoiceReadRepository or IInvoiceWriteRepository where possible (ISP).
/// </summary>
public interface IInvoiceRepository : IInvoiceReadRepository, IInvoiceWriteRepository { }
