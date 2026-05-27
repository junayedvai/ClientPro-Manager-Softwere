using System.Windows.Input;
using ClientProManager.Helpers;
using ClientProManager.Models;
using ClientProManager.Services;

namespace ClientProManager.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ICompanySettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private CompanySetting _settings = new();

    public SettingsViewModel(ICompanySettingsService settingsService, IDialogService dialogService)
    {
        _settingsService = settingsService;
        _dialogService = dialogService;
        LoadCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync());
        _ = LoadAsync();
    }

    public CompanySetting Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }

    public ICommand LoadCommand { get; }
    public ICommand SaveCommand { get; }

    private async Task LoadAsync()
    {
        Settings = await _settingsService.GetAsync();
        StatusMessage = "Company settings loaded.";
    }

    private async Task SaveAsync()
    {
        try
        {
            await _settingsService.SaveAsync(Settings);
            StatusMessage = "Company settings saved.";
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }
}
