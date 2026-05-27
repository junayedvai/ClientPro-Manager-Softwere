using System.Collections.ObjectModel;
using System.Windows.Input;
using ClientProManager.Helpers;
using ClientProManager.Models;
using ClientProManager.Services;

namespace ClientProManager.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly IDashboardService _dashboardService;
    private DashboardSummary _summary = new();

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        RefreshCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        _ = LoadAsync();
    }

    public DashboardSummary Summary
    {
        get => _summary;
        set => SetProperty(ref _summary, value);
    }

    public ObservableCollection<Invoice> RecentInvoices { get; } = [];
    public ObservableCollection<Invoice> PendingInvoices { get; } = [];
    public ObservableCollection<Invoice> OverdueInvoices { get; } = [];
    public ICommand RefreshCommand { get; }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Summary = await _dashboardService.GetSummaryAsync();
            RecentInvoices.ReplaceWith(await _dashboardService.GetRecentInvoicesAsync());
            PendingInvoices.ReplaceWith(await _dashboardService.GetPendingInvoicesAsync());
            OverdueInvoices.ReplaceWith(await _dashboardService.GetOverdueInvoicesAsync());
            StatusMessage = $"Dashboard refreshed at {DateTime.Now:t}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
