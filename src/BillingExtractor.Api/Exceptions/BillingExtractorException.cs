namespace BillingExtractor.Api.Exceptions;

/// <summary>Base class for all domain-specific exceptions in BillingExtractor.</summary>
public abstract class BillingExtractorException : Exception
{
    protected BillingExtractorException(string message) : base(message) { }
    protected BillingExtractorException(string message, Exception inner) : base(message, inner) { }
}
