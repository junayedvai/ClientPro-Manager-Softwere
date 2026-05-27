using System.Collections.ObjectModel;
using System.Windows.Input;
using ClientProManager.Helpers;
using ClientProManager.Models;
using ClientProManager.Services;

namespace ClientProManager.ViewModels;

public class ClientsViewModel : ViewModelBase
{
    private readonly IClientService _clientService;
    private readonly IPermissionService _permissionService;
    private readonly IDialogService _dialogService;
    private Client? _selectedClient;
    private Client _editableClient = NewClient();
    private string _searchText = string.Empty;
    private string _selectedCategory = "All";
    private string _selectedPaymentStatus = "All";
    private decimal _profileTotalPaid;

    public ClientsViewModel(IClientService clientService, IPermissionService permissionService, IDialogService dialogService)
    {
        _clientService = clientService;
        _permissionService = permissionService;
        _dialogService = dialogService;
        LoadCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        NewCommand = new RelayCommand(_ => EditableClient = NewClient());
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync());
        DisableCommand = new AsyncRelayCommand(async _ => await DisableAsync());
        _ = LoadAsync();
    }

    public ObservableCollection<Client> Clients { get; } = [];
    public ObservableCollection<Invoice> InvoiceHistory { get; } = [];
    public ObservableCollection<Payment> PaymentHistory { get; } = [];
    public string[] Categories { get; } = ["All", "Regular", "VIP", "New", "Supplier", "One-time"];
    public string[] PaymentStatuses { get; } = ["All", "Paid", "Due", "Partial", "Overdue"];

    public Client? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (SetProperty(ref _selectedClient, value) && value is not null)
            {
                EditableClient = Clone(value);
                _ = LoadProfileAsync(value.Id);
            }
        }
    }

    public Client EditableClient
    {
        get => _editableClient;
        set => SetProperty(ref _editableClient, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public string SelectedPaymentStatus
    {
        get => _selectedPaymentStatus;
        set => SetProperty(ref _selectedPaymentStatus, value);
    }

    public decimal ProfileTotalPaid
    {
        get => _profileTotalPaid;
        set => SetProperty(ref _profileTotalPaid, value);
    }

    public ICommand LoadCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DisableCommand { get; }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Clients.ReplaceWith(await _clientService.GetClientsAsync(SearchText, SelectedCategory, SelectedPaymentStatus));
            StatusMessage = $"{Clients.Count} client(s) loaded.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadProfileAsync(int clientId)
    {
        var profile = await _clientService.GetClientProfileAsync(clientId);
        InvoiceHistory.ReplaceWith(profile is null ? Enumerable.Empty<Invoice>() : profile.Invoices.OrderByDescending(x => x.InvoiceDate));
        PaymentHistory.ReplaceWith(profile is null ? Enumerable.Empty<Payment>() : profile.Payments.OrderByDescending(x => x.PaymentDate));
        ProfileTotalPaid = PaymentHistory.Sum(x => x.PaidAmount);
    }

    private async Task SaveAsync()
    {
        try
        {
            await _clientService.SaveClientAsync(EditableClient);
            StatusMessage = "Client saved.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    private async Task DisableAsync()
    {
        if (SelectedClient is null)
        {
            return;
        }

        if (!_permissionService.CanDeleteImportantRecords())
        {
            _dialogService.Error("Only admin users can disable client records.");
            return;
        }

        if (!_dialogService.Confirm($"Disable {SelectedClient.ClientName}?"))
        {
            return;
        }

        await _clientService.DisableClientAsync(SelectedClient.Id);
        await LoadAsync();
    }

    private static Client NewClient() => new()
    {
        ClientCategory = "Regular",
        PaymentStatus = "Due",
        LastServiceDate = DateTime.Today,
        IsActive = true
    };

    private static Client Clone(Client source) => new()
    {
        Id = source.Id,
        ClientName = source.ClientName,
        CompanyName = source.CompanyName,
        Phone = source.Phone,
        Email = source.Email,
        Address = source.Address,
        BusinessType = source.BusinessType,
        ClientCategory = source.ClientCategory,
        Notes = source.Notes,
        PreviousWorkHistory = source.PreviousWorkHistory,
        PaymentStatus = source.PaymentStatus,
        TotalDue = source.TotalDue,
        LastServiceDate = source.LastServiceDate,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
        IsActive = source.IsActive
    };
}
