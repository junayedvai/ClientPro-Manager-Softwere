using System.Collections.ObjectModel;
using System.Windows.Input;
using ClientProManager.Helpers;
using ClientProManager.Models;
using ClientProManager.Services;

namespace ClientProManager.ViewModels;

public class ProductServicesViewModel : ViewModelBase
{
    private readonly IProductCatalogService _productService;
    private readonly IPermissionService _permissionService;
    private readonly IDialogService _dialogService;
    private ProductService? _selectedItem;
    private ProductService _editableItem = NewProduct();
    private string _searchText = string.Empty;
    private string _selectedCategory = "All";

    public ProductServicesViewModel(IProductCatalogService productService, IPermissionService permissionService, IDialogService dialogService)
    {
        _productService = productService;
        _permissionService = permissionService;
        _dialogService = dialogService;
        LoadCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        NewCommand = new RelayCommand(_ => EditableItem = NewProduct());
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync());
        DisableCommand = new AsyncRelayCommand(async _ => await DisableAsync());
        _ = LoadAsync();
    }

    public ObservableCollection<ProductService> Items { get; } = [];
    public ObservableCollection<string> Categories { get; } = ["All"];

    public ProductService? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value) && value is not null)
            {
                EditableItem = Clone(value);
            }
        }
    }

    public ProductService EditableItem
    {
        get => _editableItem;
        set => SetProperty(ref _editableItem, value);
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

    public ICommand LoadCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DisableCommand { get; }

    private async Task LoadAsync()
    {
        Items.ReplaceWith(await _productService.GetProductsAsync(SearchText, SelectedCategory));
        Categories.ReplaceWith(await _productService.GetCategoriesAsync());
        StatusMessage = $"{Items.Count} product/service item(s) loaded.";
    }

    private async Task SaveAsync()
    {
        try
        {
            await _productService.SaveProductAsync(EditableItem);
            StatusMessage = "Product/service saved.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    private async Task DisableAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        if (!_permissionService.CanDeleteImportantRecords())
        {
            _dialogService.Error("Only admin users can disable products/services.");
            return;
        }

        if (!_dialogService.Confirm($"Disable {SelectedItem.Name}?"))
        {
            return;
        }

        await _productService.DisableProductAsync(SelectedItem.Id);
        await LoadAsync();
    }

    private static ProductService NewProduct() => new()
    {
        Unit = "Unit",
        Category = "General",
        TaxRate = 5,
        IsService = true,
        IsActive = true
    };

    private static ProductService Clone(ProductService source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Code = source.Code,
        Category = source.Category,
        Description = source.Description,
        Unit = source.Unit,
        UnitPrice = source.UnitPrice,
        TaxRate = source.TaxRate,
        MaterialType = source.MaterialType,
        IsService = source.IsService,
        IsProduct = source.IsProduct,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
        IsActive = source.IsActive
    };
}
