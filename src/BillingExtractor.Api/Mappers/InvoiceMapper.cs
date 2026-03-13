using System.Text.Json;
using BillingExtractor.Api.Data.Entities;
using BillingExtractor.Api.Models;

namespace BillingExtractor.Api.Mappers;

/// <inheritdoc />
public class InvoiceMapper : IInvoiceMapper
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public InvoiceRecord ToRecord(ExtractedInvoice invoice) => new()
    {
        InvoiceNumber = invoice.InvoiceNumber,
        VendorName = invoice.VendorName,
        Date = invoice.Date,
        TotalAmount = invoice.TotalAmount,
        LineItemsJson = JsonSerializer.Serialize(invoice.LineItems, _jsonOptions),
        CreatedAt = DateTime.Now
    };
}
