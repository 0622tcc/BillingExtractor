namespace BillingExtractor.Api.Models;

/// <summary>
/// Domain model representing structured data extracted from an invoice image by the LLM.
/// Computed properties encapsulate business calculations (OOP: Encapsulation).
/// </summary>
public class ExtractedInvoice
{
    public string InvoiceNumber { get; init; } = string.Empty;
    public string Date { get; init; } = string.Empty;
    public string VendorName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public List<LineItem> LineItems { get; init; } = new();

    /// <summary>Sum of all line item totals — encapsulates the calculation that consumers used to repeat.</summary>
    public decimal CalculatedTotal => LineItems.Sum(x => x.LineTotal);

    /// <summary>True when line items sum differs from stated total by more than one cent.</summary>
    public bool HasAmountMismatch => Math.Abs(CalculatedTotal - TotalAmount) > 0.01m;
}
