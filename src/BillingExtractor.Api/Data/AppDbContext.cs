using BillingExtractor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BillingExtractor.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<InvoiceRecord> Invoices => Set<InvoiceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InvoiceRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.VendorName).HasMaxLength(200);
            entity.Property(e => e.Date).HasMaxLength(20);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.InvoiceNumber);
        });
    }
}
