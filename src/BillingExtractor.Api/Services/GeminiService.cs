using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BillingExtractor.Api.Exceptions;
using BillingExtractor.Api.Models;
using BillingExtractor.Api.Options;
using BillingExtractor.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;

namespace BillingExtractor.Api.Services;

/// <summary>
/// Google Gemini implementation of ILlmService.
/// Uses Polly for resilience (retry on transient LlmTransientException).
/// Throws LlmParsingException when the AI response cannot be deserialized.
/// </summary>
public class GeminiService : ILlmService
{
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiService> _logger;
    private readonly ResiliencePipeline _pipeline;
    private readonly IHttpClientFactory _httpClientFactory;

    private const string GeminiEndpoint =
        "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";

    private const string SystemPrompt = """
        You are an invoice data extraction specialist.
        Analyze the invoice image and extract ALL fields accurately.
        Return ONLY a valid JSON object with NO markdown formatting, NO code blocks, NO explanation.

        Required JSON structure:
        {
          "invoiceNumber": "string",
          "date": "YYYY-MM-DD",
          "vendorName": "string",
          "totalAmount": 0.00,
          "lineItems": [
            {
              "description": "string",
              "quantity": 0,
              "unitPrice": 0.00,
              "lineTotal": 0.00
            }
          ]
        }

        Rules:
        - totalAmount and all monetary values must be numbers, NOT strings
        - date must be formatted as YYYY-MM-DD
        - Extract every single line item visible
        - If a field cannot be found, use null
        - Do NOT include currency symbols in numeric fields
        """;

    public GeminiService(
        IOptions<GeminiOptions> options,
        ILogger<GeminiService> logger,
        ResiliencePipelineProvider<string> pipelineProvider,
        IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _logger = logger;
        _pipeline = pipelineProvider.GetPipeline("gemini-retry");
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ExtractedInvoice> ExtractInvoiceAsync(byte[] imageBytes, string mimeType)
    {
        var base64Image = Convert.ToBase64String(imageBytes);
        var dataUrl = $"data:{mimeType};base64,{base64Image}";

        var requestBody = new
        {
            model = _options.Model,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "image_url", image_url = new { url = dataUrl } },
                        new { type = "text", text = "Extract all invoice data from this image and return only JSON." }
                    }
                }
            }
        };

        var jsonBody = JsonSerializer.Serialize(requestBody);
        string? rawContent = null;

        await _pipeline.ExecuteAsync(async (ct) =>
        {
            _logger.LogInformation("Calling Gemini API with model {Model}", _options.Model);

            var httpClient = _httpClientFactory.CreateClient("gemini");

            var request = new HttpRequestMessage(HttpMethod.Post, GeminiEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                var statusCode = (int)response.StatusCode;

                _logger.LogError("Gemini API error {Status}: {Body}", statusCode, errorBody);

                // Only retry on 429 (rate limit) or 5xx (server errors)
                if (statusCode == 429 || statusCode >= 500)
                    throw new LlmTransientException($"Gemini API {statusCode}: {errorBody}");

                // For 4xx errors, fail immediately — no retry
                throw new InvalidOperationException($"Gemini API error {statusCode}: {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            _logger.LogDebug("Raw Gemini response: {Response}", responseJson);

            using var doc = JsonDocument.Parse(responseJson);
            rawContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        });

        if (rawContent == null)
            throw new InvalidOperationException("No content received from Gemini API.");

        // Strip markdown fences if present
        var extractedJson = rawContent.Trim();
        if (extractedJson.StartsWith("```"))
        {
            var firstNewline = extractedJson.IndexOf('\n');
            var lastFence = extractedJson.LastIndexOf("```");
            if (firstNewline >= 0 && lastFence > firstNewline)
                extractedJson = extractedJson[(firstNewline + 1)..lastFence].Trim();
        }

        try
        {
            var result = JsonSerializer.Deserialize<ExtractedInvoice>(extractedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
                throw new LlmParsingException("Deserialized invoice was null.", new InvalidOperationException());

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini response as JSON: {Json}", extractedJson);
            throw new LlmParsingException(
                $"Failed to parse invoice JSON from Gemini response: {ex.Message}", ex);
        }
    }
}
