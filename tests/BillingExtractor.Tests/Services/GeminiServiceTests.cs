using System.Text.Json;
using BillingExtractor.Api.Models;
using Xunit;

namespace BillingExtractor.Tests.Services;

/// <summary>
/// Tests for JSON parsing logic extracted from GeminiService.
/// The actual OpenAI client calls require integration tests.
/// </summary>
public class GeminiServiceTests
{
    private static ExtractedInvoice? ParseInvoiceJson(string json)
    {
        // Strip markdown fences (same logic as GeminiService)
        var cleaned = json.Trim();
        if (cleaned.StartsWith("```"))
        {
            var firstNewline = cleaned.IndexOf('\n');
            var lastFence = cleaned.LastIndexOf("```");
            if (firstNewline >= 0 && lastFence > firstNewline)
                cleaned = cleaned[(firstNewline + 1)..lastFence].Trim();
        }
        return JsonSerializer.Deserialize<ExtractedInvoice>(cleaned, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    [Fact]
    public void Test_ValidJsonResponse_ParsesCorrectly()
    {
        var json = """
            {
              "invoiceNumber": "INV-2024-001",
              "date": "2024-01-15",
              "vendorName": "TechSupplies Co.",
              "totalAmount": 200.00,
              "lineItems": [
                { "description": "Laptop Stand", "quantity": 1, "unitPrice": 45.00, "lineTotal": 45.00 },
                { "description": "USB-C Hub", "quantity": 2, "unitPrice": 35.00, "lineTotal": 70.00 }
              ]
            }
            """;

        var result = ParseInvoiceJson(json);

        Assert.NotNull(result);
        Assert.Equal("INV-2024-001", result!.InvoiceNumber);
        Assert.Equal("TechSupplies Co.", result.VendorName);
        Assert.Equal(200.00m, result.TotalAmount);
        Assert.Equal(2, result.LineItems.Count);
        Assert.Equal(45.00m, result.LineItems[0].LineTotal);
    }

    [Fact]
    public void Test_JsonWithMarkdownFences_StripsFencesAndParses()
    {
        var json = """
            ```json
            {
              "invoiceNumber": "INV-001",
              "date": "2024-01-15",
              "vendorName": "Test Co",
              "totalAmount": 100.00,
              "lineItems": []
            }
            ```
            """;

        var result = ParseInvoiceJson(json);

        Assert.NotNull(result);
        Assert.Equal("INV-001", result!.InvoiceNumber);
        Assert.Equal(100.00m, result.TotalAmount);
    }

    [Fact]
    public void Test_InvalidJson_ThrowsException()
    {
        var invalidJson = "this is not json at all {{{";

        Assert.Throws<JsonException>(() => ParseInvoiceJson(invalidJson));
    }

    [Fact]
    public void Test_EmptyLineItems_ReturnsEmptyList()
    {
        var json = """
            {
              "invoiceNumber": "INV-002",
              "date": "2024-01-20",
              "vendorName": "Office Depot",
              "totalAmount": 95.00,
              "lineItems": []
            }
            """;

        var result = ParseInvoiceJson(json);

        Assert.NotNull(result);
        Assert.Empty(result!.LineItems);
    }
}
