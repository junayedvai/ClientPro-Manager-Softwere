using ClientProManager.Data;
using ClientProManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Services;

public interface IReportService
{
    string[] ReportTypes { get; }
    Task<List<ReportRow>> BuildReportAsync(string reportType, DateTime from, DateTime to);
}

public class ReportService(IDbContextFactory<ClientProDbContext> dbFactory) : IReportService
{
    public string[] ReportTypes { get; } =
    [
        "Daily Sales",
        "Monthly Sales",
        "Yearly Sales",
        "Client-wise",
        "Due Payments",
        "Expense",
        "Profit/Loss",
        "Product/Service",
        "Staff Activity"
    ];

    public async Task<List<ReportRow>> BuildReportAsync(string reportType, DateTime from, DateTime to)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var fromDate = from.Date;
        var toDate = to.Date;

        return reportType switch
        {
            "Daily Sales" => await DailySalesAsync(db, fromDate, toDate),
            "Monthly Sales" => await MonthlySalesAsync(db, fromDate, toDate),
            "Yearly Sales" => await YearlySalesAsync(db, fromDate, toDate),
            "Client-wise" => await ClientWiseAsync(db, fromDate, toDate),
            "Due Payments" => await DuePaymentsAsync(db, fromDate, toDate),
            "Expense" => await ExpenseAsync(db, fromDate, toDate),
            "Profit/Loss" => await ProfitLossAsync(db, fromDate, toDate),
            "Product/Service" => await ProductServiceAsync(db, fromDate, toDate),
            "Staff Activity" => await StaffActivityAsync(db, fromDate, toDate),
            _ => []
        };
    }

    private static async Task<List<ReportRow>> DailySalesAsync(ClientProDbContext db, DateTime from, DateTime to)
    {
        var rows = await db.Payments
            .Where(x => x.PaymentDate.Date >= from && x.PaymentDate.Date <= to)
            .GroupBy(x => x.PaymentDate.Date)
            .Select(g => new ReportRow
            {
                Label = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(),
                Income = g.Sum(x => x.PaidAmount)
            })
            .ToListAsync();

        return rows.OrderBy(x => x.Label).ToList();
    }

    private static async Task<List<ReportRow>> MonthlySalesAsync(ClientProDbContext db, DateTime from, DateTime to)
    {
        var rows = await db.Payments
            .Where(x => x.PaymentDate.Date >= from && x.PaymentDate.Date <= to)
            .GroupBy(x => new { x.PaymentDate.Year, x.PaymentDate.Month })
            .Select(g => new ReportRow
            {
                Label = $"{g.Key.Year}-{g.Key.Month:00}",
                Count = g.Count(),
                Income = g.Sum(x => x.PaidAmount)
            })
            .ToListAsync();

        return rows.OrderBy(x => x.Label).ToList();
    }

    private static async Task<List<ReportRow>> YearlySalesAsync(ClientProDbContext db, DateTime from, DateTime to)
    {
        return await db.Payments
            .Where(x => x.PaymentDate.Date >= from && x.PaymentDate.Date <= to)
            .GroupBy(x => x.PaymentDate.Year)
            .Select(g => new ReportRow
            {
                Label = g.Key.ToString(),
                Count = g.Count(),
                Income = g.Sum(x => x.PaidAmount)
            })
            .OrderBy(x => x.Label)
            .ToListAsync();
    }

    private static async Task<List<ReportRow>> ClientWiseAsync(ClientProDbContext db, DateTime from, DateTime to)
    {
        return await db.Invoices
            .Include(x => x.Client)
            .Where(x => x.InvoiceDate.Date >= from && x.InvoiceDate.Date <= to && x.Status != InvoiceStatuses.Cancelled)
            .GroupBy(x => new { x.ClientId, x.Client!.ClientName })
            .Select(g => new ReportRow
            {
                Label = g.Key.ClientName,
                Count = g.Count(),
                Income = g.Sum(x => x.PaidAmount),
                Due = g.Sum(x => x.DueAmount)
            })
            .OrderByDescending(x => x.Income)
            .ToListAsync();
    }

    private static async Task<List<ReportRow>> DuePaymentsAsync(ClientProDbContext db, DateTime from, DateTime to)
    {
        return await db.Invoices
            .Include(x => x.Client)
            .Where(x => x.DueDate.Date >= from && x.DueDate.Date <= to && x.DueAmount > 0 && x.Status != InvoiceStatuses.Cancelled)
            .Select(x => new ReportRow
            {
                Label = x.InvoiceNumber,
                SecondaryLabel = x.Client!.ClientName,
                Count = 1,
                Income = x.GrandTotal,
                Due = x.DueAmount
            })
            .OrderByDescending(x => x.Due)
            .ToListAsync();
    }

    private static async Task<List<ReportRow>> ExpenseAsync(ClientProDbContext db, DateTime from, DateTime to)
    {
        return await db.Expenses
            .Where(x => x.ExpenseDate.Date >= from && x.ExpenseDate.Date <= to)
            .GroupBy(x => x.Category)
            .Select(g => new ReportRow
            {
                Label = g.Key,
                Count = g.Count(),
                Expense = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Expense)
            .ToListAsync();
    }

    private static async Task<List<ReportRow>> ProfitLossAsync(ClientProDbContext db, DateTime from, DateTime to)
    {
        var income = await db.Payments.Where(x => x.PaymentDate.Date >= from && x.PaymentDate.Date <= to).SumAsync(x => x.PaidAmount);
        var expense = await db.Expenses.Where(x => x.ExpenseDate.Date >= from && x.ExpenseDate.Date <= to).SumAsync(x => x.Amount);
        return
        [
            new ReportRow
            {
                Label = "Profit/Loss",
                Count = 1,
                Income = income,
                Expense = expense
            }
        ];
    }

    private static async Task<List<ReportRow>> ProductServiceAsync(ClientProDbContext db, DateTime from, DateTime to)
    {
        return await db.InvoiceItems
            .Include(x => x.Invoice)
            .Include(x => x.ProductService)
            .Where(x => x.Invoice!.InvoiceDate.Date >= from && x.Invoice.InvoiceDate.Date <= to && x.Invoice.Status != InvoiceStatuses.Cancelled)
            .GroupBy(x => new { x.ProductServiceId, x.ProductService!.Name })
            .Select(g => new ReportRow
            {
                Label = g.Key.Name,
                Count = g.Count(),
                Income = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.Income)
            .ToListAsync();
    }

    private static async Task<List<ReportRow>> StaffActivityAsync(ClientProDbContext db, DateTime from, DateTime to)
    {
        return await db.ActivityLogs
            .Include(x => x.User)
            .Where(x => x.CreatedAt.Date >= from && x.CreatedAt.Date <= to)
            .GroupBy(x => new { x.UserId, UserName = x.User != null ? x.User.Username : "System" })
            .Select(g => new ReportRow
            {
                Label = g.Key.UserName,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
    }
}
