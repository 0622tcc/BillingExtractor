using System.Text.Json;
using BillingExtractor.Api.Data;
using BillingExtractor.Api.Data.Entities;
using BillingExtractor.Api.Mappers;
using BillingExtractor.Api.Models;
using BillingExtractor.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BillingExtractor.Tests.Services;

public class InvoiceRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly InvoiceRepository _sut;

    public InvoiceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new InvoiceRepository(_db, new InvoiceMapper());
    }

    public void Dispose() => _db.Dispose();

    // --- FindByInvoiceNumberAsync ---

    [Fact]
    public async Task FindByInvoiceNumber_ExistingRecord_ReturnsRecord()
    {
        _db.Invoices.Add(new InvoiceRecord
        {
            InvoiceNumber = "INV-001",
            VendorName = "Test Co",
            Date = "2024-01-15",
            TotalAmount = 100m,
            LineItemsJson = "[]"
        });
        await _db.SaveChangesAsync();

        var result = await _sut.FindByInvoiceNumberAsync("INV-001");

        Assert.NotNull(result);
        Assert.Equal("INV-001", result!.InvoiceNumber);
    }

    [Fact]
    public async Task FindByInvoiceNumber_CaseInsensitive_ReturnsRecord()
    {
        _db.Invoices.Add(new InvoiceRecord { InvoiceNumber = "inv-001", VendorName = "V", Date = "2024-01-01", LineItemsJson = "[]" });
        await _db.SaveChangesAsync();

        var result = await _sut.FindByInvoiceNumberAsync("INV-001");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task FindByInvoiceNumber_NotFound_ReturnsNull()
    {
        var result = await _sut.FindByInvoiceNumberAsync("NONEXISTENT");
        Assert.Null(result);
    }

    // --- SaveAsync ---

    [Fact]
    public async Task SaveAsync_PersistsRecord_WithSerializedLineItems()
    {
        var invoice = new ExtractedInvoice
        {
            InvoiceNumber = "INV-SAVE-001",
            VendorName = "Acme Corp",
            Date = "2024-02-01",
            TotalAmount = 150m,
            LineItems = new List<LineItem>
            {
                new() { Description = "Widget", Quantity = 3, UnitPrice = 50m, LineTotal = 150m }
            }
        };

        await _sut.SaveAsync(invoice);

        var saved = await _sut.FindByInvoiceNumberAsync("INV-SAVE-001");
        Assert.NotNull(saved);
        Assert.Equal("INV-SAVE-001", saved!.InvoiceNumber);
        Assert.Equal("Acme Corp", saved.VendorName);
        Assert.Equal(150m, saved.TotalAmount);

        var items = JsonSerializer.Deserialize<List<LineItem>>(saved.LineItemsJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(items);
        Assert.Single(items!);
        Assert.Equal("Widget", items![0].Description);
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_ReturnsRecordsOrderedByCreatedAtDesc()
    {
        _db.Invoices.AddRange(
            new InvoiceRecord { InvoiceNumber = "INV-A", VendorName = "A", Date = "2024-01-01", CreatedAt = DateTime.UtcNow.AddDays(-2), LineItemsJson = "[]" },
            new InvoiceRecord { InvoiceNumber = "INV-B", VendorName = "B", Date = "2024-01-02", CreatedAt = DateTime.UtcNow.AddDays(-1), LineItemsJson = "[]" },
            new InvoiceRecord { InvoiceNumber = "INV-C", VendorName = "C", Date = "2024-01-03", CreatedAt = DateTime.UtcNow, LineItemsJson = "[]" }
        );
        await _db.SaveChangesAsync();

        var results = (await _sut.GetAllAsync()).ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal("INV-C", results[0].InvoiceNumber);
        Assert.Equal("INV-A", results[2].InvoiceNumber);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_ExistingRecord_ReturnsTrueAndRemovesRecord()
    {
        _db.Invoices.Add(new InvoiceRecord { Id = 10, InvoiceNumber = "INV-DEL", VendorName = "V", Date = "2024-01-01", LineItemsJson = "[]" });
        await _db.SaveChangesAsync();

        var deleted = await _sut.DeleteAsync(10);

        Assert.True(deleted);
        Assert.Null(await _db.Invoices.FindAsync(10));
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        var deleted = await _sut.DeleteAsync(9999);

        Assert.False(deleted);
    }
}
