using System.Collections.ObjectModel;
using System.Windows.Input;
using ClientProManager.Helpers;
using ClientProManager.Models;
using ClientProManager.Services;

namespace ClientProManager.ViewModels;

public class QuotationsViewModel : ViewModelBase
{
    private readonly IQuotationService _quotationService;
    private readonly IClientService _clientService;
    private readonly IProductCatalogService _productService;
    private readonly IInvoiceService _invoiceService;
    private readonly IExportService _exportService;
    private readonly IDialogService _dialogService;
    private Quotation? _selectedQuotation;
    private Quotation _currentQuotation = new();
    private Client? _selectedClient;
    private ProductService? _selectedProduct;
    private QuotationItem? _selectedLine;
    private decimal _lineQuantity = 1;
    private decimal _lineDiscount;
    private decimal _lineTaxRate = 5;

    public QuotationsViewModel(
        IQuotationService quotationService,
        IClientService clientService,
        IProductCatalogService productService,
        IInvoiceService invoiceService,
        IExportService exportService,
        IDialogService dialogService)
    {
        _quotationService = quotationService;
        _clientService = clientService;
        _productService = productService;
        _invoiceService = invoiceService;
        _exportService = exportService;
        _dialogService = dialogService;

        LoadCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        NewCommand = new AsyncRelayCommand(async _ => await NewAsync());
        AddLineCommand = new RelayCommand(_ => AddLine());
        RemoveLineCommand = new RelayCommand(_ => RemoveLine());
        RecalculateCommand = new RelayCommand(_ => Recalculate());
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync());
        ExportPdfCommand = new AsyncRelayCommand(async _ => await ExportPdfAsync());
        ConvertToInvoiceCommand = new AsyncRelayCommand(async _ => await ConvertToInvoiceAsync());
        _ = InitializeAsync();
    }

    public ObservableCollection<Quotation> Quotations { get; } = [];
    public ObservableCollection<Client> Clients { get; } = [];
    public ObservableCollection<ProductService> Products { get; } = [];
    public ObservableCollection<QuotationItem> Lines { get; } = [];
    public string[] Statuses { get; } = QuotationStatuses.All;

    public Quotation? SelectedQuotation
    {
        get => _selectedQuotation;
        set
        {
            if (SetProperty(ref _selectedQuotation, value) && value is not null)
            {
                LoadFromQuotation(value);
            }
        }
    }

    public Quotation CurrentQuotation
    {
        get => _currentQuotation;
        set
        {
            if (SetProperty(ref _currentQuotation, value))
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

    public QuotationItem? SelectedLine
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

    public decimal Subtotal => CurrentQuotation.Subtotal;
    public decimal DiscountAmount => CurrentQuotation.DiscountAmount;
    public decimal TaxAmount => CurrentQuotation.TaxAmount;
    public decimal GrandTotal => CurrentQuotation.GrandTotal;

    public ICommand LoadCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand RecalculateCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand ConvertToInvoiceCommand { get; }

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
        Quotations.ReplaceWith(await _quotationService.GetQuotationsAsync());
        StatusMessage = $"{Quotations.Count} quotation(s) loaded.";
    }

    private async Task NewAsync()
    {
        CurrentQuotation = new Quotation
        {
            QuotationNumber = await _quotationService.GenerateQuotationNumberAsync(),
            QuotationDate = DateTime.Today,
            ValidUntil = DateTime.Today.AddDays(15),
            Status = QuotationStatuses.Draft
        };
        SelectedClient = Clients.FirstOrDefault();
        Lines.Clear();
        OnTotalsChanged();
    }

    private void LoadFromQuotation(Quotation quotation)
    {
        CurrentQuotation = new Quotation
        {
            Id = quotation.Id,
            QuotationNumber = quotation.QuotationNumber,
            ClientId = quotation.ClientId,
            QuotationDate = quotation.QuotationDate,
            ValidUntil = quotation.ValidUntil,
            Subtotal = quotation.Subtotal,
            DiscountAmount = quotation.DiscountAmount,
            TaxAmount = quotation.TaxAmount,
            GrandTotal = quotation.GrandTotal,
            TermsAndConditions = quotation.TermsAndConditions,
            Status = quotation.Status,
            CreatedByUserId = quotation.CreatedByUserId,
            CreatedAt = quotation.CreatedAt,
            UpdatedAt = quotation.UpdatedAt
        };
        SelectedClient = Clients.FirstOrDefault(x => x.Id == quotation.ClientId);
        Lines.ReplaceWith(quotation.Items.Select(x => new QuotationItem
        {
            Id = x.Id,
            QuotationId = x.QuotationId,
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

        Lines.Add(new QuotationItem
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
            CurrentQuotation.Items = Lines.ToList();
            _quotationService.Calculate(CurrentQuotation);
            Lines.ReplaceWith(CurrentQuotation.Items);
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
            CurrentQuotation.ClientId = SelectedClient?.Id ?? 0;
            CurrentQuotation.Items = Lines.ToList();
            await _quotationService.SaveQuotationAsync(CurrentQuotation);
            StatusMessage = "Quotation saved.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    private async Task ExportPdfAsync()
    {
        if (CurrentQuotation.Id == 0)
        {
            _dialogService.Error("Save the quotation before exporting.");
            return;
        }

        var path = await _exportService.ExportQuotationPdfAsync(CurrentQuotation.Id);
        _dialogService.Info($"Quotation PDF exported:\n{path}");
    }

    private async Task ConvertToInvoiceAsync()
    {
        if (SelectedQuotation is null)
        {
            return;
        }

        var invoice = await _invoiceService.ConvertQuotationToInvoiceAsync(SelectedQuotation.Id);
        _dialogService.Info($"Created invoice {invoice.InvoiceNumber}.");
        await LoadAsync();
    }

    private void OnTotalsChanged()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(DiscountAmount));
        OnPropertyChanged(nameof(TaxAmount));
        OnPropertyChanged(nameof(GrandTotal));
    }
}
