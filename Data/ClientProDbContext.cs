using ClientProManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Data;

public class ClientProDbContext(DbContextOptions<ClientProDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientDocument> ClientDocuments => Set<ClientDocument>();
    public DbSet<ProductService> ProductServices => Set<ProductService>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<CompanySetting> CompanySettings => Set<CompanySetting>();
    public DbSet<BackupLog> BackupLogs => Set<BackupLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<Invoice>().HasIndex(x => x.InvoiceNumber).IsUnique();
        modelBuilder.Entity<Quotation>().HasIndex(x => x.QuotationNumber).IsUnique();
        modelBuilder.Entity<Payment>().HasIndex(x => x.PaymentNumber).IsUnique();
        modelBuilder.Entity<ProductService>().HasIndex(x => x.Code).IsUnique();

        ConfigureMoney<Client>(modelBuilder, x => x.TotalDue);
        ConfigureMoney<ProductService>(modelBuilder, x => x.UnitPrice);
        ConfigureMoney<ProductService>(modelBuilder, x => x.TaxRate);
        ConfigureMoney<Quotation>(modelBuilder, x => x.Subtotal);
        ConfigureMoney<Quotation>(modelBuilder, x => x.DiscountAmount);
        ConfigureMoney<Quotation>(modelBuilder, x => x.TaxAmount);
        ConfigureMoney<Quotation>(modelBuilder, x => x.GrandTotal);
        ConfigureMoney<QuotationItem>(modelBuilder, x => x.Quantity);
        ConfigureMoney<QuotationItem>(modelBuilder, x => x.UnitPrice);
        ConfigureMoney<QuotationItem>(modelBuilder, x => x.Discount);
        ConfigureMoney<QuotationItem>(modelBuilder, x => x.TaxRate);
        ConfigureMoney<QuotationItem>(modelBuilder, x => x.LineTotal);
        ConfigureMoney<Invoice>(modelBuilder, x => x.Subtotal);
        ConfigureMoney<Invoice>(modelBuilder, x => x.DiscountAmount);
        ConfigureMoney<Invoice>(modelBuilder, x => x.TaxAmount);
        ConfigureMoney<Invoice>(modelBuilder, x => x.GrandTotal);
        ConfigureMoney<Invoice>(modelBuilder, x => x.PaidAmount);
        ConfigureMoney<Invoice>(modelBuilder, x => x.DueAmount);
        ConfigureMoney<InvoiceItem>(modelBuilder, x => x.Quantity);
        ConfigureMoney<InvoiceItem>(modelBuilder, x => x.UnitPrice);
        ConfigureMoney<InvoiceItem>(modelBuilder, x => x.Discount);
        ConfigureMoney<InvoiceItem>(modelBuilder, x => x.TaxRate);
        ConfigureMoney<InvoiceItem>(modelBuilder, x => x.LineTotal);
        ConfigureMoney<Payment>(modelBuilder, x => x.PaidAmount);
        ConfigureMoney<Payment>(modelBuilder, x => x.DueAmountAfterPayment);
        ConfigureMoney<Expense>(modelBuilder, x => x.Amount);
        ConfigureMoney<CompanySetting>(modelBuilder, x => x.DefaultTaxRate);

        modelBuilder.Entity<ClientDocument>()
            .HasOne(x => x.Client)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuotationItem>()
            .HasOne(x => x.Quotation)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InvoiceItem>()
            .HasOne(x => x.Invoice)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Payment>()
            .HasOne(x => x.Invoice)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(x => x.Client)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureMoney<TEntity>(
        ModelBuilder modelBuilder,
        System.Linq.Expressions.Expression<Func<TEntity, decimal>> propertyExpression)
        where TEntity : class
    {
        modelBuilder.Entity<TEntity>().Property(propertyExpression).HasColumnType("decimal(18,2)");
    }
}
