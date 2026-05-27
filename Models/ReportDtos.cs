namespace ClientProManager.Models;

public class DashboardSummary
{
    public int TotalClients { get; set; }
    public int TotalInvoices { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalDueAmount { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpense { get; set; }
    public decimal NetProfit => MonthlyIncome - MonthlyExpense;
}

public class ReportRow
{
    public string Label { get; set; } = string.Empty;
    public string SecondaryLabel { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Due { get; set; }
    public decimal Profit => Income - Expense;
}
