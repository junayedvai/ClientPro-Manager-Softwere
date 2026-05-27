using ClientProManager.Data;
using ClientProManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Services;

public interface IExpenseService
{
    Task<List<Expense>> GetExpensesAsync(DateTime? from = null, DateTime? to = null, string? category = null, string? paidBy = null);
    Task SaveExpenseAsync(Expense expense);
    Task DeleteExpenseAsync(int id);
}

public class ExpenseService(
    IDbContextFactory<ClientProDbContext> dbFactory,
    ISessionService sessionService,
    IActivityLogService activityLogService) : IExpenseService
{
    public async Task<List<Expense>> GetExpensesAsync(DateTime? from = null, DateTime? to = null, string? category = null, string? paidBy = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.Expenses.AsNoTracking();

        if (from.HasValue)
        {
            query = query.Where(x => x.ExpenseDate.Date >= from.Value.Date);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.ExpenseDate.Date <= to.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(x => x.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(paidBy) && paidBy != "All")
        {
            query = query.Where(x => x.PaidBy == paidBy);
        }

        return await query.OrderByDescending(x => x.ExpenseDate).ToListAsync();
    }

    public async Task SaveExpenseAsync(Expense expense)
    {
        if (string.IsNullOrWhiteSpace(expense.ExpenseType))
        {
            throw new InvalidOperationException("Expense type is required.");
        }

        if (expense.Amount <= 0)
        {
            throw new InvalidOperationException("Amount must be greater than zero.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        expense.CreatedByUserId = expense.CreatedByUserId == 0 ? sessionService.CurrentUser?.Id ?? 1 : expense.CreatedByUserId;
        expense.UpdatedAt = DateTime.Now;

        if (expense.Id == 0)
        {
            expense.CreatedAt = DateTime.Now;
            db.Expenses.Add(expense);
        }
        else
        {
            db.Expenses.Update(expense);
        }

        await db.SaveChangesAsync();
        await activityLogService.LogAsync("Save Expense", "Expense", expense.Id, expense.ExpenseType);
    }

    public async Task DeleteExpenseAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var expense = await db.Expenses.FirstAsync(x => x.Id == id);
        db.Expenses.Remove(expense);
        await db.SaveChangesAsync();
        await activityLogService.LogAsync("Delete Expense", "Expense", id, expense.ExpenseType);
    }
}
