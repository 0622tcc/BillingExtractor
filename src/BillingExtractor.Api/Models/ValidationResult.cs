namespace BillingExtractor.Api.Models;

public class ValidationResult
{
    public bool IsDuplicate { get; set; }
    public bool HasAmountMismatch { get; set; }
    public decimal CalculatedTotal { get; set; }
    public decimal StatedTotal { get; set; }
    public List<string> Warnings { get; set; } = new();
}
