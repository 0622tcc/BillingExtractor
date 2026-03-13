namespace BillingExtractor.Api.Models;

/// <summary>A single line on an invoice — immutable once deserialized from LLM output.</summary>
public class LineItem
{
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}
