using ClientProManager.Data;
using ClientProManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Services;

public interface IPaymentService
{
    Task<List<Payment>> GetPaymentsAsync(DateTime? from = null, DateTime? to = null, int? clientId = null, string? method = null);
    Task<string> GeneratePaymentNumberAsync();
    Task AddPaymentAsync(Payment payment, bool allowOverpayment);
}

public class PaymentService(
    IDbContextFactory<ClientProDbContext> dbFactory,
    ISessionService sessionService,
    IClientService clientService,
    IActivityLogService activityLogService) : IPaymentService
{
    public async Task<List<Payment>> GetPaymentsAsync(DateTime? from = null, DateTime? to = null, int? clientId = null, string? method = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.Payments
            .Include(x => x.Client)
            .Include(x => x.Invoice)
            .AsNoTracking();

        if (from.HasValue)
        {
            query = query.Where(x => x.PaymentDate.Date >= from.Value.Date);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.PaymentDate.Date <= to.Value.Date);
        }

        if (clientId.HasValue)
        {
            query = query.Where(x => x.ClientId == clientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(method) && method != "All")
        {
            query = query.Where(x => x.PaymentMethod == method);
        }

        return await query.OrderByDescending(x => x.PaymentDate).ToListAsync();
    }

    public async Task<string> GeneratePaymentNumberAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var settings = await db.CompanySettings.AsNoTracking().FirstOrDefaultAsync();
        var prefix = settings?.PaymentPrefix ?? "PAY";
        var today = DateTime.Today.ToString("yyyyMMdd");
        var count = await db.Payments.CountAsync(x => x.PaymentNumber.StartsWith($"{prefix}-{today}")) + 1;
        return $"{prefix}-{today}-{count:0000}";
    }

    public async Task AddPaymentAsync(Payment payment, bool allowOverpayment)
    {
        if (payment.InvoiceId <= 0)
        {
            throw new InvalidOperationException("Invoice is required.");
        }

        if (payment.PaidAmount <= 0)
        {
            throw new InvalidOperationException("Paid amount must be greater than zero.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var invoice = await db.Invoices.FirstAsync(x => x.Id == payment.InvoiceId);

        if (payment.PaidAmount > invoice.DueAmount && !allowOverpayment)
        {
            throw new InvalidOperationException("Payment cannot be greater than the invoice due amount.");
        }

        payment.PaymentNumber = string.IsNullOrWhiteSpace(payment.PaymentNumber)
            ? await GeneratePaymentNumberAsync()
            : payment.PaymentNumber;
        payment.ClientId = invoice.ClientId;
        payment.ReceivedByUserId = sessionService.CurrentUser?.Id ?? 1;
        payment.CreatedAt = DateTime.Now;

        invoice.PaidAmount += payment.PaidAmount;
        invoice.DueAmount = invoice.GrandTotal - invoice.PaidAmount;
        if (invoice.DueAmount <= 0)
        {
            invoice.Status = InvoiceStatuses.Paid;
        }
        else if (invoice.PaidAmount > 0)
        {
            invoice.Status = invoice.DueDate.Date < DateTime.Today ? InvoiceStatuses.Overdue : InvoiceStatuses.Partial;
        }
        else
        {
            invoice.Status = InvoiceStatuses.Unpaid;
        }

        payment.DueAmountAfterPayment = invoice.DueAmount;

        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        await clientService.RecalculateClientDueAsync(invoice.ClientId);
        await activityLogService.LogAsync("Add Payment", "Payment", payment.Id, $"{payment.PaymentNumber} for {invoice.InvoiceNumber}");
    }
}
