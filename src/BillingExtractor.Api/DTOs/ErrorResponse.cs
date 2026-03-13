namespace BillingExtractor.Api.DTOs;

/// <summary>Standard error envelope returned on 4xx/5xx responses.</summary>
public record ErrorResponse(string Error);
