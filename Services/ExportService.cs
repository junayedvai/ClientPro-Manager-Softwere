using System.Globalization;
using System.IO;
using System.Text;
using ClientProManager.Data;
using ClientProManager.Helpers;
using ClientProManager.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClientProManager.Services;

public interface IExportService
{
    Task<string> ExportInvoicePdfAsync(int invoiceId);
    Task<string> ExportQuotationPdfAsync(int quotationId);
    Task<string> ExportPaymentReceiptPdfAsync(int paymentId);
    Task<string> ExportReportPdfAsync(string reportType, IReadOnlyCollection<ReportRow> rows, DateTime from, DateTime to);
    Task<string> ExportReportExcelAsync(string reportType, IReadOnlyCollection<ReportRow> rows, DateTime from, DateTime to);
    Task<string> ExportReportCsvAsync(string reportType, IReadOnlyCollection<ReportRow> rows, DateTime from, DateTime to);
}

public class ExportService(
    IDbContextFactory<ClientProDbContext> dbFactory,
    IActivityLogService activityLogService) : IExportService
{
    private static readonly CultureInfo BdCulture = CultureInfo.GetCultureInfo("en-BD");

    public async Task<string> ExportInvoicePdfAsync(int invoiceId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var company = await GetCompanyAsync(db);
        var invoice = await db.Invoices
            .Include(x => x.Client)
            .Include(x => x.Items)
            .ThenInclude(x => x.ProductService)
            .AsNoTracking()
            .FirstAsync(x => x.Id == invoiceId);

        var path = GetExportPath($"Invoice_{invoice.InvoiceNumber}.pdf");
        Document.Create(container => ComposeInvoice(container, company, invoice)).GeneratePdf(path);
        await activityLogService.LogAsync("Export Invoice PDF", "Invoice", invoice.Id, path);
        return path;
    }

    public async Task<string> ExportQuotationPdfAsync(int quotationId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var company = await GetCompanyAsync(db);
        var quotation = await db.Quotations
            .Include(x => x.Client)
            .Include(x => x.Items)
            .ThenInclude(x => x.ProductService)
            .AsNoTracking()
            .FirstAsync(x => x.Id == quotationId);

        var path = GetExportPath($"Quotation_{quotation.QuotationNumber}.pdf");
        Document.Create(container => ComposeQuotation(container, company, quotation)).GeneratePdf(path);
        await activityLogService.LogAsync("Export Quotation PDF", "Quotation", quotation.Id, path);
        return path;
    }

    public async Task<string> ExportPaymentReceiptPdfAsync(int paymentId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var company = await GetCompanyAsync(db);
        var payment = await db.Payments
            .Include(x => x.Client)
            .Include(x => x.Invoice)
            .AsNoTracking()
            .FirstAsync(x => x.Id == paymentId);

        var path = GetExportPath($"PaymentReceipt_{payment.PaymentNumber}.pdf");
        Document.Create(container => ComposePaymentReceipt(container, company, payment)).GeneratePdf(path);
        await activityLogService.LogAsync("Export Payment Receipt", "Payment", payment.Id, path);
        return path;
    }

    public Task<string> ExportReportPdfAsync(string reportType, IReadOnlyCollection<ReportRow> rows, DateTime from, DateTime to)
    {
        var path = GetExportPath($"{SafeName(reportType)}_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        Document.Create(container => ComposeReport(container, reportType, rows, from, to)).GeneratePdf(path);
        return Task.FromResult(path);
    }

    public Task<string> ExportReportExcelAsync(string reportType, IReadOnlyCollection<ReportRow> rows, DateTime from, DateTime to)
    {
        var path = GetExportPath($"{SafeName(reportType)}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Report");
        worksheet.Cell(1, 1).Value = reportType;
        worksheet.Cell(2, 1).Value = $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}";
        worksheet.Cell(4, 1).Value = "Label";
        worksheet.Cell(4, 2).Value = "Details";
        worksheet.Cell(4, 3).Value = "Count";
        worksheet.Cell(4, 4).Value = "Income";
        worksheet.Cell(4, 5).Value = "Expense";
        worksheet.Cell(4, 6).Value = "Due";
        worksheet.Cell(4, 7).Value = "Profit";

        var row = 5;
        foreach (var item in rows)
        {
            worksheet.Cell(row, 1).Value = item.Label;
            worksheet.Cell(row, 2).Value = item.SecondaryLabel;
            worksheet.Cell(row, 3).Value = item.Count;
            worksheet.Cell(row, 4).Value = item.Income;
            worksheet.Cell(row, 5).Value = item.Expense;
            worksheet.Cell(row, 6).Value = item.Due;
            worksheet.Cell(row, 7).Value = item.Profit;
            row++;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
        return Task.FromResult(path);
    }

    public Task<string> ExportReportCsvAsync(string reportType, IReadOnlyCollection<ReportRow> rows, DateTime from, DateTime to)
    {
        var path = GetExportPath($"{SafeName(reportType)}_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        var builder = new StringBuilder();
        builder.AppendLine("Label,Details,Count,Income,Expense,Due,Profit");
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',',
                Csv(row.Label),
                Csv(row.SecondaryLabel),
                row.Count,
                row.Income.ToString(CultureInfo.InvariantCulture),
                row.Expense.ToString(CultureInfo.InvariantCulture),
                row.Due.ToString(CultureInfo.InvariantCulture),
                row.Profit.ToString(CultureInfo.InvariantCulture)));
        }

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
        return Task.FromResult(path);
    }

    private static async Task<CompanySetting> GetCompanyAsync(ClientProDbContext db)
    {
        return await db.CompanySettings.AsNoTracking().FirstOrDefaultAsync() ?? new CompanySetting();
    }

    private static string GetExportPath(string fileName)
    {
        AppPaths.EnsureFolders();
        return Path.Combine(AppPaths.ExportFolder, fileName.Replace(':', '-'));
    }

    private static string SafeName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value.Replace(' ', '_').Replace('/', '_');
    }

    private static string Money(decimal value) => $"{value.ToString("N2", BdCulture)} BDT";

    private static string Csv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static void ComposeInvoice(IDocumentContainer container, CompanySetting company, Invoice invoice)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(32);
            page.DefaultTextStyle(x => x.FontSize(10));
            page.Header().Element(c => ComposeDocumentHeader(c, company, "INVOICE", invoice.InvoiceNumber, invoice.InvoiceDate, invoice.DueDate));
            page.Content().Column(column =>
            {
                column.Spacing(14);
                column.Item().Element(c => ComposeClientBlock(c, invoice.Client));
                column.Item().Element(c => ComposeInvoiceItems(c, invoice.Items));
                column.Item().AlignRight().Width(220).Element(c => ComposeTotals(c, invoice.Subtotal, invoice.DiscountAmount, invoice.TaxAmount, invoice.GrandTotal, invoice.PaidAmount, invoice.DueAmount));
                column.Item().Text(invoice.Notes);
                column.Item().Text(company.TermsAndConditions).FontSize(9).FontColor(Colors.Grey.Darken2);
            });
            page.Footer().AlignRight().Text("Authorized Signature __________________");
        });
    }

    private static void ComposeQuotation(IDocumentContainer container, CompanySetting company, Quotation quotation)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(32);
            page.DefaultTextStyle(x => x.FontSize(10));
            page.Header().Element(c => ComposeDocumentHeader(c, company, "QUOTATION", quotation.QuotationNumber, quotation.QuotationDate, quotation.ValidUntil));
            page.Content().Column(column =>
            {
                column.Spacing(14);
                column.Item().Element(c => ComposeClientBlock(c, quotation.Client));
                column.Item().Element(c => ComposeQuotationItems(c, quotation.Items));
                column.Item().AlignRight().Width(220).Element(c => ComposeTotals(c, quotation.Subtotal, quotation.DiscountAmount, quotation.TaxAmount, quotation.GrandTotal, 0, quotation.GrandTotal));
                column.Item().Text(quotation.TermsAndConditions);
            });
            page.Footer().AlignRight().Text("Authorized Signature __________________");
        });
    }

    private static void ComposePaymentReceipt(IDocumentContainer container, CompanySetting company, Payment payment)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(32);
            page.Header().Element(c => ComposeDocumentHeader(c, company, "PAYMENT RECEIPT", payment.PaymentNumber, payment.PaymentDate, null));
            page.Content().Column(column =>
            {
                column.Spacing(12);
                column.Item().Element(c => ComposeClientBlock(c, payment.Client));
                column.Item().Text($"Invoice: {payment.Invoice?.InvoiceNumber}").SemiBold();
                column.Item().Text($"Payment Method: {payment.PaymentMethod}");
                column.Item().Text($"Paid Amount: {Money(payment.PaidAmount)}").FontSize(16).Bold();
                column.Item().Text($"Due After Payment: {Money(payment.DueAmountAfterPayment)}");
                column.Item().Text(payment.TransactionNote);
            });
            page.Footer().AlignRight().Text("Received By __________________");
        });
    }

    private static void ComposeReport(IDocumentContainer container, string reportType, IReadOnlyCollection<ReportRow> rows, DateTime from, DateTime to)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(32);
            page.Header().Column(column =>
            {
                column.Item().Text(reportType).FontSize(20).Bold();
                column.Item().Text($"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}").FontColor(Colors.Grey.Darken2);
            });
            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.ConstantColumn(45);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddReportHeader(table, "Label");
                AddReportHeader(table, "Details");
                AddReportHeader(table, "Count");
                AddReportHeader(table, "Income");
                AddReportHeader(table, "Expense");
                AddReportHeader(table, "Due");
                AddReportHeader(table, "Profit");

                foreach (var row in rows)
                {
                    AddCell(table, row.Label);
                    AddCell(table, row.SecondaryLabel);
                    AddCell(table, row.Count.ToString(CultureInfo.InvariantCulture));
                    AddCell(table, Money(row.Income));
                    AddCell(table, Money(row.Expense));
                    AddCell(table, Money(row.Due));
                    AddCell(table, Money(row.Profit));
                }
            });
        });
    }

    private static void ComposeDocumentHeader(IContainer container, CompanySetting company, string title, string number, DateTime date, DateTime? dueDate)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(company.CompanyName).FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                column.Item().Text(company.Address);
                column.Item().Text($"{company.Phone}  {company.Email}");
                if (!string.IsNullOrWhiteSpace(company.Website))
                {
                    column.Item().Text(company.Website);
                }
            });

            row.ConstantItem(190).Column(column =>
            {
                column.Item().AlignRight().Text(title).FontSize(22).Bold();
                column.Item().AlignRight().Text(number).FontColor(Colors.Grey.Darken2);
                column.Item().AlignRight().Text($"Date: {date:yyyy-MM-dd}");
                if (dueDate.HasValue)
                {
                    column.Item().AlignRight().Text($"Due/Valid: {dueDate:yyyy-MM-dd}");
                }
            });
        });
    }

    private static void ComposeClientBlock(IContainer container, Client? client)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("Client").Bold();
            column.Item().Text(client?.ClientName ?? "");
            column.Item().Text(client?.CompanyName ?? "");
            column.Item().Text(client?.Phone ?? "");
            column.Item().Text(client?.Email ?? "");
            column.Item().Text(client?.Address ?? "");
        });
    }

    private static void ComposeInvoiceItems(IContainer container, IEnumerable<InvoiceItem> items)
    {
        container.Table(table =>
        {
            ComposeItemColumns(table);
            AddItemHeader(table, "Description");
            AddItemHeader(table, "Qty");
            AddItemHeader(table, "Unit");
            AddItemHeader(table, "Rate");
            AddItemHeader(table, "Discount");
            AddItemHeader(table, "Tax %");
            AddItemHeader(table, "Total");

            foreach (var item in items)
            {
                AddCell(table, item.Description);
                AddCell(table, item.Quantity.ToString("N2"));
                AddCell(table, item.Unit);
                AddCell(table, Money(item.UnitPrice));
                AddCell(table, Money(item.Discount));
                AddCell(table, item.TaxRate.ToString("N2"));
                AddCell(table, Money(item.LineTotal));
            }
        });
    }

    private static void ComposeQuotationItems(IContainer container, IEnumerable<QuotationItem> items)
    {
        container.Table(table =>
        {
            ComposeItemColumns(table);
            AddItemHeader(table, "Description");
            AddItemHeader(table, "Qty");
            AddItemHeader(table, "Unit");
            AddItemHeader(table, "Rate");
            AddItemHeader(table, "Discount");
            AddItemHeader(table, "Tax %");
            AddItemHeader(table, "Total");

            foreach (var item in items)
            {
                AddCell(table, item.Description);
                AddCell(table, item.Quantity.ToString("N2"));
                AddCell(table, item.Unit);
                AddCell(table, Money(item.UnitPrice));
                AddCell(table, Money(item.Discount));
                AddCell(table, item.TaxRate.ToString("N2"));
                AddCell(table, Money(item.LineTotal));
            }
        });
    }

    private static void ComposeItemColumns(TableDescriptor table)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(3);
            columns.ConstantColumn(45);
            columns.ConstantColumn(55);
            columns.RelativeColumn();
            columns.RelativeColumn();
            columns.ConstantColumn(45);
            columns.RelativeColumn();
        });
    }

    private static void ComposeTotals(IContainer container, decimal subtotal, decimal discount, decimal tax, decimal grandTotal, decimal paid, decimal due)
    {
        container.Column(column =>
        {
            AddTotalLine(column, "Subtotal", subtotal);
            AddTotalLine(column, "Discount", discount);
            AddTotalLine(column, "Tax", tax);
            AddTotalLine(column, "Grand Total", grandTotal, true);
            AddTotalLine(column, "Paid", paid);
            AddTotalLine(column, "Due", due, true);
        });
    }

    private static void AddTotalLine(ColumnDescriptor column, string label, decimal amount, bool strong = false)
    {
        column.Item().Row(row =>
        {
            row.RelativeItem().Text(label).SemiBold();
            var amountText = row.RelativeItem().AlignRight().Text(Money(amount));
            if (strong)
            {
                amountText.Bold();
            }
        });
    }

    private static void AddItemHeader(TableDescriptor table, string text)
    {
        table.Cell().Background(Colors.Blue.Darken3).Padding(4).Text(text).FontColor(Colors.White).SemiBold();
    }

    private static void AddReportHeader(TableDescriptor table, string text)
    {
        table.Cell().Background(Colors.Grey.Darken3).Padding(4).Text(text).FontColor(Colors.White).SemiBold();
    }

    private static void AddCell(TableDescriptor table, string text)
    {
        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text);
    }
}
