using BillingExtractor.Api.Models;

namespace BillingExtractor.Api.DTOs;

/// <summary>Response returned after successfully processing an invoice image.</summary>
public record ExtractResponse(ExtractedInvoice Invoice, ValidationResult Validation, bool Saved);
