using BillingExtractor.Api.Data;
using BillingExtractor.Api.Data.Entities;
using BillingExtractor.Api.Mappers;
using BillingExtractor.Api.Models;
using BillingExtractor.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BillingExtractor.Api.Services;

/// <summary>
/// EF Core–backed implementation of IInvoiceRepository.
/// Delegates domain-to-entity mapping to IInvoiceMapper (SRP).
/// Works with InvoiceRecord (infrastructure entity in Data.Entities)
/// while exposing only domain types through its interface contracts.
/// </summary>
public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _db;
    private readonly IInvoiceMapper _mapper;

    public InvoiceRepository(AppDbContext db, IInvoiceMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<InvoiceRecord?> FindByInvoiceNumberAsync(string invoiceNumber)
        => await _db.Invoices
            .FirstOrDefaultAsync(x => x.InvoiceNumber.ToLower() == invoiceNumber.ToLower());

    public async Task SaveAsync(ExtractedInvoice invoice)
    {
        var record = _mapper.ToRecord(invoice);
        _db.Invoices.Add(record);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var record = await _db.Invoices.FindAsync(id);
        if (record is null) return false;

        _db.Invoices.Remove(record);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<InvoiceRecord>> GetAllAsync()
        => await _db.Invoices
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
}
