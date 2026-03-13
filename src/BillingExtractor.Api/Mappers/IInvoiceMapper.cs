using BillingExtractor.Api.Data.Entities;
using BillingExtractor.Api.Models;

namespace BillingExtractor.Api.Mappers;

/// <summary>Responsible for mapping domain models to persistence entities (SRP).</summary>
public interface IInvoiceMapper
{
    /// <summary>Converts an AI-extracted domain model into a persistable EF Core entity.</summary>
    InvoiceRecord ToRecord(ExtractedInvoice invoice);
}
