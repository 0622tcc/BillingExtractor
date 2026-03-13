namespace BillingExtractor.Api.Exceptions;

/// <summary>
/// Thrown for transient LLM errors (429 rate limit, 5xx server errors) that Polly should retry.
/// </summary>
public class LlmTransientException : BillingExtractorException
{
    public LlmTransientException(string message) : base(message) { }
}
