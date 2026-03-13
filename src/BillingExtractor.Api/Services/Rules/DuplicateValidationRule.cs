using BillingExtractor.Api.Models;
using BillingExtractor.Api.Services.Interfaces;

namespace BillingExtractor.Api.Services.Rules;

/// <summary>
/// Checks whether an invoice with the same number has already been persisted (ISP: uses IInvoiceReadRepository).
/// </summary>
public class DuplicateValidationRule : IInvoiceValidationRule
{
    private readonly IInvoiceReadRepository _readRepository;

    public DuplicateValidationRule(IInvoiceReadRepository readRepository)
        => _readRepository = readRepository;

    public async Task ApplyAsync(ExtractedInvoice invoice, ValidationResult result)
    {
        var existing = await _readRepository.FindByInvoiceNumberAsync(invoice.InvoiceNumber);
        if (existing != null)
        {
            result.IsDuplicate = true;
            result.Warnings.Add($"Invoice #{invoice.InvoiceNumber} was already submitted on {existing.Date}");
        }
    }
}
