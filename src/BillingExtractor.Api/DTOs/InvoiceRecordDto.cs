using System.Text.Json;
using BillingExtractor.Api.Data.Entities;
using BillingExtractor.Api.Models;

namespace BillingExtractor.Api.DTOs;

/// <summary>
/// HTTP read model returned by GET /api/invoices.
/// Maps from the persistence entity (InvoiceRecord) to a JSON-serializable response
/// with deserialized line items — a controller/presentation concern (pure DTO).
/// </summary>
public class InvoiceRecordDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<LineItem> LineItems { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InvoiceRecordDto(InvoiceRecord record)
    {
        Id = record.Id;
        InvoiceNumber = record.InvoiceNumber;
        VendorName = record.VendorName;
        Date = record.Date;
        TotalAmount = record.TotalAmount;
        CreatedAt = record.CreatedAt;
        LineItems = string.IsNullOrEmpty(record.LineItemsJson)
            ? new List<LineItem>()
            : JsonSerializer.Deserialize<List<LineItem>>(record.LineItemsJson, _jsonOptions)
              ?? new List<LineItem>();
    }
}
