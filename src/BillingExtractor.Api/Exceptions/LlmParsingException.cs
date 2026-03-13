namespace BillingExtractor.Api.Exceptions;

/// <summary>
/// Thrown when the LLM response cannot be parsed as a valid invoice JSON structure.
/// </summary>
public class LlmParsingException : BillingExtractorException
{
    public LlmParsingException(string message, Exception inner) : base(message, inner) { }
}
