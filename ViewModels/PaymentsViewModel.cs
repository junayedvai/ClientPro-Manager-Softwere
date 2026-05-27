using System.Collections.ObjectModel;
using System.Windows.Input;
using ClientProManager.Helpers;
using ClientProManager.Models;
using ClientProManager.Services;

namespace ClientProManager.ViewModels;

public class PaymentsViewModel : ViewModelBase
{
    private readonly IPaymentService _paymentService;
    private readonly IInvoiceService _invoiceService;
    private readonly IPermissionService _permissionService;
    private readonly IExportService _exportService;
    private readonly IDialogService _dialogService;
    private Invoice? _selectedInvoice;
    private Payment? _selectedPayment;
    private DateTime _fromDate = DateTime.Today.AddMonths(-1);
    private DateTime _toDate = DateTime.Today;
    private string _selectedMethod = "All";
    private Payment _newPayment = new() { PaymentDate = DateTime.Today, PaymentMethod = "Cash" };

    public PaymentsViewModel(
        IPaymentService paymentService,
        IInvoiceService invoiceService,
        IPermissionService permissionService,
        IExportService exportService,
        IDialogService dialogService)
    {
        _paymentService = paymentService;
        _invoiceService = invoiceService;
        _permissionService = permissionService;
        _exportService = exportService;
        _dialogService = dialogService;
        LoadCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        AddPaymentCommand = new AsyncRelayCommand(async _ => await AddPaymentAsync());
        ExportReceiptCommand = new AsyncRelayCommand(async _ => await ExportReceiptAsync());
        _ = LoadAsync();
    }

    public ObservableCollection<Payment> Payments { get; } = [];
    public ObservableCollection<Invoice> DueInvoices { get; } = [];
    public string[] Methods { get; } = ["All", .. ClientProManager.Models.PaymentMethods.All];

    public Invoice? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (SetProperty(ref _selectedInvoice, value) && value is not null)
            {
                NewPayment.InvoiceId = value.Id;
                NewPayment.ClientId = value.ClientId;
                NewPayment.PaidAmount = value.DueAmount;
                OnPropertyChanged(nameof(NewPayment));
            }
        }
    }

    public Payment? SelectedPayment
    {
        get => _selectedPayment;
        set => SetProperty(ref _selectedPayment, value);
    }

    public Payment NewPayment
    {
        get => _newPayment;
        set => SetProperty(ref _newPayment, value);
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

    public string SelectedMethod
    {
        get => _selectedMethod;
        set => SetProperty(ref _selectedMethod, value);
    }

    public ICommand LoadCommand { get; }
    public ICommand AddPaymentCommand { get; }
    public ICommand ExportReceiptCommand { get; }

    private async Task LoadAsync()
    {
        Payments.ReplaceWith(await _paymentService.GetPaymentsAsync(FromDate, ToDate, null, SelectedMethod));
        DueInvoices.ReplaceWith(await _invoiceService.GetDueInvoicesAsync());
        StatusMessage = $"{Payments.Count} payment(s) loaded.";
    }

    private async Task AddPaymentAsync()
    {
        try
        {
            if (SelectedInvoice is null)
            {
                _dialogService.Error("Select an invoice.");
                return;
            }

            await _paymentService.AddPaymentAsync(NewPayment, _permissionService.CanDeleteImportantRecords());
            NewPayment = new Payment { PaymentDate = DateTime.Today, PaymentMethod = "Cash" };
            StatusMessage = "Payment added.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    private async Task ExportReceiptAsync()
    {
        if (SelectedPayment is null)
        {
            return;
        }

        var path = await _exportService.ExportPaymentReceiptPdfAsync(SelectedPayment.Id);
        _dialogService.Info($"Payment receipt exported:\n{path}");
    }
}
