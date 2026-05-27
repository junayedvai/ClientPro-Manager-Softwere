using ClientProManager.Data;
using ClientProManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Services;

public interface IInvoiceService
{
    Task<List<Invoice>> GetInvoicesAsync(bool includeCancelled = true);
    Task<List<Invoice>> GetDueInvoicesAsync();
    Task<string> GenerateInvoiceNumberAsync();
    void Calculate(Invoice invoice);
    void UpdateInvoiceStatus(Invoice invoice);
    Task SaveInvoiceAsync(Invoice invoice);
    Task<Invoice> ConvertQuotationToInvoiceAsync(int quotationId);
}

public class InvoiceService(
    IDbContextFactory<ClientProDbContext> dbFactory,
    ISessionService sessionService,
    IClientService clientService,
    IActivityLogService activityLogService) : IInvoiceService
{
    public async Task<List<Invoice>> GetInvoicesAsync(bool includeCancelled = true)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.Invoices
            .Include(x => x.Client)
            .Include(x => x.Items)
            .ThenInclude(x => x.ProductService)
            .AsNoTracking();

        if (!includeCancelled)
        {
            query = query.Where(x => x.Status != InvoiceStatuses.Cancelled);
        }

        return await query.OrderByDescending(x => x.InvoiceDate).ToListAsync();
    }

    public async Task<List<Invoice>> GetDueInvoicesAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Invoices
            .Include(x => x.Client)
            .Where(x => x.Status != InvoiceStatuses.Paid && x.Status != InvoiceStatuses.Cancelled && x.DueAmount > 0)
            .OrderBy(x => x.DueDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<string> GenerateInvoiceNumberAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var settings = await db.CompanySettings.AsNoTracking().FirstOrDefaultAsync();
        var prefix = settings?.InvoicePrefix ?? "INV";
        var today = DateTime.Today.ToString("yyyyMMdd");
        var count = await db.Invoices.CountAsync(x => x.InvoiceNumber.StartsWith($"{prefix}-{today}")) + 1;
        return $"{prefix}-{today}-{count:0000}";
    }

    public void Calculate(Invoice invoice)
    {
        foreach (var item in invoice.Items)
        {
            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException("Quantity must be greater than zero.");
            }

            var baseAmount = item.Quantity * item.UnitPrice;
            var taxableAmount = Math.Max(0, baseAmount - item.Discount);
            var taxAmount = taxableAmount * item.TaxRate / 100m;
            item.LineTotal = taxableAmount + taxAmount;
        }

        invoice.Subtotal = invoice.Items.Sum(x => x.Quantity * x.UnitPrice);
        invoice.DiscountAmount = invoice.Items.Sum(x => x.Discount);
        invoice.TaxAmount = invoice.Items.Sum(x => Math.Max(0, (x.Quantity * x.UnitPrice) - x.Discount) * x.TaxRate / 100m);
        invoice.GrandTotal = invoice.Subtotal - invoice.DiscountAmount + invoice.TaxAmount;
        invoice.DueAmount = invoice.GrandTotal - invoice.PaidAmount;
        UpdateInvoiceStatus(invoice);
    }

    public void UpdateInvoiceStatus(Invoice invoice)
    {
        if (invoice.Status == InvoiceStatuses.Cancelled)
        {
            return;
        }

        if (invoice.DueAmount <= 0)
        {
            invoice.Status = InvoiceStatuses.Paid;
        }
        else if (invoice.DueDate.Date < DateTime.Today && invoice.DueAmount > 0)
        {
            invoice.Status = InvoiceStatuses.Overdue;
        }
        else if (invoice.PaidAmount > 0)
        {
            invoice.Status = InvoiceStatuses.Partial;
        }
        else
        {
            invoice.Status = InvoiceStatuses.Unpaid;
        }
    }

    public async Task SaveInvoiceAsync(Invoice invoice)
    {
        if (invoice.ClientId <= 0)
        {
            throw new InvalidOperationException("Client is required.");
        }

        if (invoice.DueDate.Date < invoice.InvoiceDate.Date)
        {
            throw new InvalidOperationException("Due date cannot be before invoice date.");
        }

        if (!invoice.Items.Any())
        {
            throw new InvalidOperationException("Add at least one invoice item.");
        }

        Calculate(invoice);

        await using var db = await dbFactory.CreateDbContextAsync();
        var duplicate = await db.Invoices.AnyAsync(x => x.InvoiceNumber == invoice.InvoiceNumber && x.Id != invoice.Id);
        if (duplicate)
        {
            throw new InvalidOperationException("Invoice number must be unique.");
        }

        invoice.CreatedByUserId = invoice.CreatedByUserId == 0 ? sessionService.CurrentUser?.Id ?? 1 : invoice.CreatedByUserId;
        invoice.UpdatedAt = DateTime.Now;

        foreach (var item in invoice.Items)
        {
            item.ProductService = null;
            item.Invoice = null;
        }

        if (invoice.Id == 0)
        {
            invoice.CreatedAt = DateTime.Now;
            db.Invoices.Add(invoice);
        }
        else
        {
            var existing = await db.Invoices.Include(x => x.Items).FirstAsync(x => x.Id == invoice.Id);
            db.InvoiceItems.RemoveRange(existing.Items);
            db.Entry(existing).CurrentValues.SetValues(invoice);
            foreach (var item in invoice.Items)
            {
                item.Id = 0;
                existing.Items.Add(item);
            }
        }

        await db.SaveChangesAsync();
        await clientService.RecalculateClientDueAsync(invoice.ClientId);
        await activityLogService.LogAsync("Create/Update Invoice", "Invoice", invoice.Id, invoice.InvoiceNumber);
    }

    public async Task<Invoice> ConvertQuotationToInvoiceAsync(int quotationId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var quotation = await db.Quotations
            .Include(x => x.Items)
            .FirstAsync(x => x.Id == quotationId);

        var invoice = new Invoice
        {
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            ClientId = quotation.ClientId,
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(7),
            Notes = $"Converted from quotation {quotation.QuotationNumber}",
            CreatedByUserId = sessionService.CurrentUser?.Id ?? quotation.CreatedByUserId,
            Items = quotation.Items.Select(x => new InvoiceItem
            {
                ProductServiceId = x.ProductServiceId,
                Description = x.Description,
                Quantity = x.Quantity,
                Unit = x.Unit,
                UnitPrice = x.UnitPrice,
                Discount = x.Discount,
                TaxRate = x.TaxRate
            }).ToList()
        };

        Calculate(invoice);
        db.Invoices.Add(invoice);
        quotation.Status = QuotationStatuses.Converted;
        quotation.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
        await clientService.RecalculateClientDueAsync(invoice.ClientId);
        await activityLogService.LogAsync("Convert Quotation", "Quotation", quotation.Id, $"{quotation.QuotationNumber} to {invoice.InvoiceNumber}");
        return invoice;
    }
}
