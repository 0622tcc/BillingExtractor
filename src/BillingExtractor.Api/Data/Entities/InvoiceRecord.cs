using System.ComponentModel.DataAnnotations.Schema;

namespace BillingExtractor.Api.Data.Entities;

/// <summary>
/// EF Core persistence entity — represents a row in the Invoices table.
/// This is an INFRASTRUCTURE concern. Domain logic lives in ExtractedInvoice.
/// Intentionally kept separate from domain models in Models/.
/// </summary>
[Table("Invoices")]
public class InvoiceRecord
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    /// <summary>Line items serialized as JSON — EF Core cannot map List&lt;LineItem&gt; natively with SQLite.</summary>
    public string LineItemsJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
