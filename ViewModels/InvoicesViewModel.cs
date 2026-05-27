using System.Collections.ObjectModel;
using System.Windows.Input;
using ClientProManager.Helpers;
using ClientProManager.Models;
using ClientProManager.Services;

namespace ClientProManager.ViewModels;

public class InvoicesViewModel : ViewModelBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IClientService _clientService;
    private readonly IProductCatalogService _productService;
    private readonly IExportService _exportService;
    private readonly IDialogService _dialogService;
    private Invoice? _selectedInvoice;
    private Invoice _currentInvoice = new();
    private Client? _selectedClient;
    private ProductService? _selectedProduct;
    private InvoiceItem? _selectedLine;
    private decimal _lineQuantity = 1;
    private decimal _lineDiscount;
    private decimal _lineTaxRate = 5;

    public InvoicesViewModel(
        IInvoiceService invoiceService,
        IClientService clientService,
        IProductCatalogService productService,
        IExportService exportService,
        IDialogService dialogService)
    {
        _invoiceService = invoiceService;
        _clientService = clientService;
        _productService = productService;
        _exportService = exportService;
        _dialogService = dialogService;

        LoadCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        NewCommand = new AsyncRelayCommand(async _ => await NewAsync());
        AddLineCommand = new RelayCommand(_ => AddLine());
        RemoveLineCommand = new RelayCommand(_ => RemoveLine());
        RecalculateCommand = new RelayCommand(_ => Recalculate());
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync());
        ExportPdfCommand = new AsyncRelayCommand(async _ => await ExportPdfAsync());
        _ = InitializeAsync();
    }

    public ObservableCollection<Invoice> Invoices { get; } = [];
    public ObservableCollection<Client> Clients { get; } = [];
    public ObservableCollection<ProductService> Products { get; } = [];
    public ObservableCollection<InvoiceItem> Lines { get; } = [];
    public string[] Statuses { get; } = InvoiceStatuses.All;
    public string[] PaymentMethods { get; } = ClientProManager.Models.PaymentMethods.All;

    public Invoice? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (SetProperty(ref _selectedInvoice, value) && value is not null)
            {
                LoadFromInvoice(value);
            }
        }
    }

    public Invoice CurrentInvoice
    {
        get => _currentInvoice;
        set
        {
            if (SetProperty(ref _currentInvoice, value))
            {
                OnTotalsChanged();
            }
        }
    }

    public Client? SelectedClient
    {
        get => _selectedClient;
        set => SetProperty(ref _selectedClient, value);
    }

    public ProductService? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value) && value is not null)
            {
                LineTaxRate = value.TaxRate;
            }
        }
    }

    public InvoiceItem? SelectedLine
    {
        get => _selectedLine;
        set => SetProperty(ref _selectedLine, value);
    }

    public decimal LineQuantity
    {
        get => _lineQuantity;
        set => SetProperty(ref _lineQuantity, value);
    }

    public decimal LineDiscount
    {
        get => _lineDiscount;
        set => SetProperty(ref _lineDiscount, value);
    }

    public decimal LineTaxRate
    {
        get => _lineTaxRate;
        set => SetProperty(ref _lineTaxRate, value);
    }

    public decimal Subtotal => CurrentInvoice.Subtotal;
    public decimal DiscountAmount => CurrentInvoice.DiscountAmount;
    public decimal TaxAmount => CurrentInvoice.TaxAmount;
    public decimal GrandTotal => CurrentInvoice.GrandTotal;
    public decimal PaidAmount => CurrentInvoice.PaidAmount;
    public decimal DueAmount => CurrentInvoice.DueAmount;

    public ICommand LoadCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand RecalculateCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ExportPdfCommand { get; }

    private async Task InitializeAsync()
    {
        await LoadLookupsAsync();
        await NewAsync();
        await LoadAsync();
    }

    private async Task LoadLookupsAsync()
    {
        Clients.ReplaceWith(await _clientService.GetClientsAsync());
        Products.ReplaceWith(await _productService.GetProductsAsync());
    }

    private async Task LoadAsync()
    {
        Invoices.ReplaceWith(await _invoiceService.GetInvoicesAsync());
        StatusMessage = $"{Invoices.Count} invoice(s) loaded.";
    }

    private async Task NewAsync()
    {
        CurrentInvoice = new Invoice
        {
            InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(),
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(7),
            PaymentMethod = "Cash",
            Status = InvoiceStatuses.Unpaid
        };
        SelectedClient = Clients.FirstOrDefault();
        Lines.Clear();
        OnTotalsChanged();
    }

    private void LoadFromInvoice(Invoice invoice)
    {
        CurrentInvoice = new Invoice
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            ClientId = invoice.ClientId,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            Subtotal = invoice.Subtotal,
            DiscountAmount = invoice.DiscountAmount,
            TaxAmount = invoice.TaxAmount,
            GrandTotal = invoice.GrandTotal,
            PaidAmount = invoice.PaidAmount,
            DueAmount = invoice.DueAmount,
            PaymentMethod = invoice.PaymentMethod,
            Status = invoice.Status,
            Notes = invoice.Notes,
            CreatedByUserId = invoice.CreatedByUserId,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt
        };
        SelectedClient = Clients.FirstOrDefault(x => x.Id == invoice.ClientId);
        Lines.ReplaceWith(invoice.Items.Select(x => new InvoiceItem
        {
            Id = x.Id,
            InvoiceId = x.InvoiceId,
            ProductServiceId = x.ProductServiceId,
            Description = x.Description,
            Quantity = x.Quantity,
            Unit = x.Unit,
            UnitPrice = x.UnitPrice,
            Discount = x.Discount,
            TaxRate = x.TaxRate,
            LineTotal = x.LineTotal
        }));
        OnTotalsChanged();
    }

    private void AddLine()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        Lines.Add(new InvoiceItem
        {
            ProductServiceId = SelectedProduct.Id,
            Description = SelectedProduct.Description == string.Empty ? SelectedProduct.Name : SelectedProduct.Description,
            Quantity = LineQuantity <= 0 ? 1 : LineQuantity,
            Unit = SelectedProduct.Unit,
            UnitPrice = SelectedProduct.UnitPrice,
            Discount = LineDiscount,
            TaxRate = LineTaxRate
        });
        Recalculate();
    }

    private void RemoveLine()
    {
        if (SelectedLine is not null)
        {
            Lines.Remove(SelectedLine);
            Recalculate();
        }
    }

    private void Recalculate()
    {
        try
        {
            CurrentInvoice.Items = Lines.ToList();
            _invoiceService.Calculate(CurrentInvoice);
            Lines.ReplaceWith(CurrentInvoice.Items);
            OnTotalsChanged();
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            CurrentInvoice.ClientId = SelectedClient?.Id ?? 0;
            CurrentInvoice.Items = Lines.ToList();
            await _invoiceService.SaveInvoiceAsync(CurrentInvoice);
            StatusMessage = "Invoice saved.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    private async Task ExportPdfAsync()
    {
        if (CurrentInvoice.Id == 0)
        {
            _dialogService.Error("Save the invoice before exporting.");
            return;
        }

        var path = await _exportService.ExportInvoicePdfAsync(CurrentInvoice.Id);
        _dialogService.Info($"Invoice PDF exported:\n{path}");
    }

    private void OnTotalsChanged()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(DiscountAmount));
        OnPropertyChanged(nameof(TaxAmount));
        OnPropertyChanged(nameof(GrandTotal));
        OnPropertyChanged(nameof(PaidAmount));
        OnPropertyChanged(nameof(DueAmount));
    }
}
