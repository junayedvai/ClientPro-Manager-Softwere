using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ClientProManager.Helpers;
using ClientProManager.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClientProManager.ViewModels;

public class NavigationItem(string key, string title)
{
    public string Key { get; } = key;
    public string Title { get; } = title;
}

public class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;
    private readonly IPermissionService _permissionService;
    private readonly ISessionService _sessionService;
    private NavigationItem? _selectedNavigationItem;
    private ViewModelBase? _currentViewModel;

    public MainViewModel(IServiceProvider services, IPermissionService permissionService, ISessionService sessionService)
    {
        _services = services;
        _permissionService = permissionService;
        _sessionService = sessionService;
        LogoutCommand = new RelayCommand(_ => Application.Current.Shutdown());

        var items = new[]
        {
            new NavigationItem("Dashboard", "Dashboard"),
            new NavigationItem("Clients", "Clients"),
            new NavigationItem("Products", "Products/Services"),
            new NavigationItem("Quotations", "Quotations"),
            new NavigationItem("Invoices", "Invoices"),
            new NavigationItem("Payments", "Payments"),
            new NavigationItem("Expenses", "Expenses"),
            new NavigationItem("Reports", "Reports"),
            new NavigationItem("Users", "Users & Roles"),
            new NavigationItem("Backup", "Backup/Restore"),
            new NavigationItem("Settings", "Company Settings")
        };

        foreach (var item in items.Where(x => _permissionService.CanAccess(x.Key)))
        {
            NavigationItems.Add(item);
        }

        SelectedNavigationItem = NavigationItems.FirstOrDefault();
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; } = [];

    public NavigationItem? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (SetProperty(ref _selectedNavigationItem, value) && value is not null)
            {
                Navigate(value.Key);
            }
        }
    }

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public string CurrentUserDisplay =>
        $"{_sessionService.CurrentUser?.FullName ?? "Unknown"} ({_sessionService.CurrentUser?.Role ?? ""})";

    public ICommand LogoutCommand { get; }

    private void Navigate(string key)
    {
        if (!_permissionService.CanAccess(key))
        {
            StatusMessage = "You do not have permission to open this module.";
            return;
        }

        CurrentViewModel = key switch
        {
            "Dashboard" => _services.GetRequiredService<DashboardViewModel>(),
            "Clients" => _services.GetRequiredService<ClientsViewModel>(),
            "Products" => _services.GetRequiredService<ProductServicesViewModel>(),
            "Quotations" => _services.GetRequiredService<QuotationsViewModel>(),
            "Invoices" => _services.GetRequiredService<InvoicesViewModel>(),
            "Payments" => _services.GetRequiredService<PaymentsViewModel>(),
            "Expenses" => _services.GetRequiredService<ExpensesViewModel>(),
            "Reports" => _services.GetRequiredService<ReportsViewModel>(),
            "Users" => _services.GetRequiredService<UsersViewModel>(),
            "Backup" => _services.GetRequiredService<BackupViewModel>(),
            "Settings" => _services.GetRequiredService<SettingsViewModel>(),
            _ => _services.GetRequiredService<DashboardViewModel>()
        };
    }
}
