using ClientProManager.Data;
using ClientProManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Services;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync();
    Task<List<Invoice>> GetRecentInvoicesAsync(int take = 8);
    Task<List<Invoice>> GetPendingInvoicesAsync(int take = 8);
    Task<List<Invoice>> GetOverdueInvoicesAsync(int take = 8);
}

public class DashboardService(IDbContextFactory<ClientProDbContext> dbFactory) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        return new DashboardSummary
        {
            TotalClients = await db.Clients.CountAsync(x => x.IsActive),
            TotalInvoices = await db.Invoices.CountAsync(x => x.Status != InvoiceStatuses.Cancelled),
            TotalPaidAmount = await db.Invoices.Where(x => x.Status != InvoiceStatuses.Cancelled).SumAsync(x => x.PaidAmount),
            TotalDueAmount = await db.Invoices.Where(x => x.Status != InvoiceStatuses.Cancelled).SumAsync(x => x.DueAmount),
            MonthlyIncome = await db.Payments.Where(x => x.PaymentDate.Date >= start && x.PaymentDate.Date <= end).SumAsync(x => x.PaidAmount),
            MonthlyExpense = await db.Expenses.Where(x => x.ExpenseDate.Date >= start && x.ExpenseDate.Date <= end).SumAsync(x => x.Amount)
        };
    }

    public async Task<List<Invoice>> GetRecentInvoicesAsync(int take = 8)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Invoices
            .Include(x => x.Client)
            .OrderByDescending(x => x.InvoiceDate)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Invoice>> GetPendingInvoicesAsync(int take = 8)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Invoices
            .Include(x => x.Client)
            .Where(x => x.DueAmount > 0 && x.Status != InvoiceStatuses.Cancelled)
            .OrderBy(x => x.DueDate)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Invoice>> GetOverdueInvoicesAsync(int take = 8)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Invoices
            .Include(x => x.Client)
            .Where(x => x.DueAmount > 0 && x.DueDate.Date < DateTime.Today && x.Status != InvoiceStatuses.Cancelled)
            .OrderBy(x => x.DueDate)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();
    }
}
