using ClientProManager.Data;
using ClientProManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Services;

public interface IQuotationService
{
    Task<List<Quotation>> GetQuotationsAsync();
    Task<string> GenerateQuotationNumberAsync();
    void Calculate(Quotation quotation);
    Task SaveQuotationAsync(Quotation quotation);
}

public class QuotationService(
    IDbContextFactory<ClientProDbContext> dbFactory,
    ISessionService sessionService,
    IActivityLogService activityLogService) : IQuotationService
{
    public async Task<List<Quotation>> GetQuotationsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Quotations
            .Include(x => x.Client)
            .Include(x => x.Items)
            .ThenInclude(x => x.ProductService)
            .OrderByDescending(x => x.QuotationDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<string> GenerateQuotationNumberAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var settings = await db.CompanySettings.AsNoTracking().FirstOrDefaultAsync();
        var prefix = settings?.QuotationPrefix ?? "QT";
        var today = DateTime.Today.ToString("yyyyMMdd");
        var count = await db.Quotations.CountAsync(x => x.QuotationNumber.StartsWith($"{prefix}-{today}")) + 1;
        return $"{prefix}-{today}-{count:0000}";
    }

    public void Calculate(Quotation quotation)
    {
        foreach (var item in quotation.Items)
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

        quotation.Subtotal = quotation.Items.Sum(x => x.Quantity * x.UnitPrice);
        quotation.DiscountAmount = quotation.Items.Sum(x => x.Discount);
        quotation.TaxAmount = quotation.Items.Sum(x => Math.Max(0, (x.Quantity * x.UnitPrice) - x.Discount) * x.TaxRate / 100m);
        quotation.GrandTotal = quotation.Subtotal - quotation.DiscountAmount + quotation.TaxAmount;
    }

    public async Task SaveQuotationAsync(Quotation quotation)
    {
        if (quotation.ClientId <= 0)
        {
            throw new InvalidOperationException("Client is required.");
        }

        if (!quotation.Items.Any())
        {
            throw new InvalidOperationException("Add at least one quotation item.");
        }

        Calculate(quotation);

        await using var db = await dbFactory.CreateDbContextAsync();
        var duplicate = await db.Quotations.AnyAsync(x => x.QuotationNumber == quotation.QuotationNumber && x.Id != quotation.Id);
        if (duplicate)
        {
            throw new InvalidOperationException("Quotation number must be unique.");
        }

        quotation.CreatedByUserId = quotation.CreatedByUserId == 0 ? sessionService.CurrentUser?.Id ?? 1 : quotation.CreatedByUserId;
        quotation.UpdatedAt = DateTime.Now;

        foreach (var item in quotation.Items)
        {
            item.ProductService = null;
            item.Quotation = null;
        }

        if (quotation.Id == 0)
        {
            quotation.CreatedAt = DateTime.Now;
            db.Quotations.Add(quotation);
        }
        else
        {
            var existing = await db.Quotations.Include(x => x.Items).FirstAsync(x => x.Id == quotation.Id);
            db.QuotationItems.RemoveRange(existing.Items);
            db.Entry(existing).CurrentValues.SetValues(quotation);
            foreach (var item in quotation.Items)
            {
                item.Id = 0;
                existing.Items.Add(item);
            }
        }

        await db.SaveChangesAsync();
        await activityLogService.LogAsync("Save Quotation", "Quotation", quotation.Id, quotation.QuotationNumber);
    }
}
