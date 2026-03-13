using BillingExtractor.Api.Models;

namespace BillingExtractor.Api.Services.Interfaces;

public interface ILlmService
{
    Task<ExtractedInvoice> ExtractInvoiceAsync(byte[] imageBytes, string mimeType);
}
