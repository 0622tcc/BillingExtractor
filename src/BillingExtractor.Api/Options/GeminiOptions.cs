namespace BillingExtractor.Api.Options;

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.0-flash";
    public int MaxRetries { get; set; } = 3;
}
