using System.Collections.ObjectModel;
using System.Windows.Input;
using ClientProManager.Helpers;
using ClientProManager.Models;
using ClientProManager.Services;

namespace ClientProManager.ViewModels;

public class ExpensesViewModel : ViewModelBase
{
    private readonly IExpenseService _expenseService;
    private readonly IPermissionService _permissionService;
    private readonly IDialogService _dialogService;
    private Expense? _selectedExpense;
    private Expense _editableExpense = NewExpense();
    private DateTime _fromDate = DateTime.Today.AddMonths(-1);
    private DateTime _toDate = DateTime.Today;
    private string _selectedCategory = "All";

    public ExpensesViewModel(IExpenseService expenseService, IPermissionService permissionService, IDialogService dialogService)
    {
        _expenseService = expenseService;
        _permissionService = permissionService;
        _dialogService = dialogService;
        LoadCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        NewCommand = new RelayCommand(_ => EditableExpense = NewExpense());
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync());
        DeleteCommand = new AsyncRelayCommand(async _ => await DeleteAsync());
        _ = LoadAsync();
    }

    public ObservableCollection<Expense> Expenses { get; } = [];
    public string[] Categories { get; } = ["All", .. ExpenseCategories.All];
    public string[] Methods { get; } = PaymentMethods.All;

    public Expense? SelectedExpense
    {
        get => _selectedExpense;
        set
        {
            if (SetProperty(ref _selectedExpense, value) && value is not null)
            {
                EditableExpense = Clone(value);
            }
        }
    }

    public Expense EditableExpense
    {
        get => _editableExpense;
        set => SetProperty(ref _editableExpense, value);
    }

    public DateTime FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public ICommand LoadCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }

    private async Task LoadAsync()
    {
        Expenses.ReplaceWith(await _expenseService.GetExpensesAsync(FromDate, ToDate, SelectedCategory));
        StatusMessage = $"{Expenses.Count} expense(s) loaded.";
    }

    private async Task SaveAsync()
    {
        try
        {
            await _expenseService.SaveExpenseAsync(EditableExpense);
            StatusMessage = "Expense saved.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedExpense is null)
        {
            return;
        }

        if (!_permissionService.CanDeleteImportantRecords())
        {
            _dialogService.Error("Only admin users can delete expenses.");
            return;
        }

        if (!_dialogService.Confirm($"Delete expense {SelectedExpense.ExpenseType}?"))
        {
            return;
        }

        await _expenseService.DeleteExpenseAsync(SelectedExpense.Id);
        await LoadAsync();
    }

    private static Expense NewExpense() => new()
    {
        ExpenseDate = DateTime.Today,
        Category = "Other",
        PaidBy = "Cash"
    };

    private static Expense Clone(Expense source) => new()
    {
        Id = source.Id,
        ExpenseType = source.ExpenseType,
        Category = source.Category,
        Amount = source.Amount,
        ExpenseDate = source.ExpenseDate,
        PaidBy = source.PaidBy,
        Note = source.Note,
        CreatedByUserId = source.CreatedByUserId,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt
    };
}
