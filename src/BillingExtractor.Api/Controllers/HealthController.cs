using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BillingExtractor.Api.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BillingExtractor.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly GeminiOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public HealthController(IOptions<GeminiOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>Checks API key validity and Gemini model availability.</summary>
    [HttpGet]
    public async Task<IActionResult> Check()
    {
        var keyPreview = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? "(not set)"
            : $"{_options.ApiKey[..Math.Min(8, _options.ApiKey.Length)]}...";

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return Ok(new { status = "degraded", model = _options.Model, keyPreview, issues = new[] { "GEMINI_API_KEY is not set." } });

        try
        {
            var httpClient = _httpClientFactory.CreateClient("gemini");

            var body = JsonSerializer.Serialize(new
            {
                model = _options.Model,
                messages = new[] { new { role = "user", content = "say hi" } },
                max_tokens = 5
            });

            var request = new HttpRequestMessage(HttpMethod.Post,
                "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return Ok(new { status = "ok", model = _options.Model, keyPreview, issues = Array.Empty<string>() });

            return Ok(new
            {
                status = "error",
                model = _options.Model,
                keyPreview,
                issues = new[] { $"Gemini returned {(int)response.StatusCode}: {responseText[..Math.Min(300, responseText.Length)]}" }
            });
        }
        catch (Exception ex)
        {
            return Ok(new { status = "error", model = _options.Model, keyPreview, issues = new[] { ex.Message } });
        }
    }
}
