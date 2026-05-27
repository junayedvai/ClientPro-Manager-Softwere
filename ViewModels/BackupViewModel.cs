using System.Collections.ObjectModel;
using System.Windows.Input;
using ClientProManager.Helpers;
using ClientProManager.Models;
using ClientProManager.Services;

namespace ClientProManager.ViewModels;

public class BackupViewModel : ViewModelBase
{
    private readonly IBackupService _backupService;
    private readonly IPermissionService _permissionService;
    private readonly IDialogService _dialogService;
    private string _restoreFilePath = string.Empty;
    private string _lastBackupPath = string.Empty;

    public BackupViewModel(IBackupService backupService, IPermissionService permissionService, IDialogService dialogService)
    {
        _backupService = backupService;
        _permissionService = permissionService;
        _dialogService = dialogService;
        BackupCommand = new AsyncRelayCommand(async _ => await BackupAsync());
        RestoreCommand = new AsyncRelayCommand(async _ => await RestoreAsync());
        CloudPlaceholderCommand = new AsyncRelayCommand(async _ => await CloudPlaceholderAsync());
        LoadCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        _ = LoadAsync();
    }

    public ObservableCollection<BackupLog> BackupLogs { get; } = [];

    public string RestoreFilePath
    {
        get => _restoreFilePath;
        set => SetProperty(ref _restoreFilePath, value);
    }

    public string LastBackupPath
    {
        get => _lastBackupPath;
        set => SetProperty(ref _lastBackupPath, value);
    }

    public ICommand BackupCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand CloudPlaceholderCommand { get; }
    public ICommand LoadCommand { get; }

    private async Task LoadAsync()
    {
        BackupLogs.ReplaceWith(await _backupService.GetBackupLogsAsync());
    }

    private async Task BackupAsync()
    {
        if (!_permissionService.CanBackupRestore())
        {
            _dialogService.Error("Only admin users can backup the database.");
            return;
        }

        LastBackupPath = await _backupService.CreateBackupAsync();
        StatusMessage = "Backup created.";
        await LoadAsync();
        _dialogService.Info($"Backup created:\n{LastBackupPath}");
    }

    private async Task RestoreAsync()
    {
        if (!_permissionService.CanBackupRestore())
        {
            _dialogService.Error("Only admin users can restore the database.");
            return;
        }

        if (!_dialogService.Confirm("Restore will overwrite the current database. Continue?"))
        {
            return;
        }

        await _backupService.RestoreBackupAsync(RestoreFilePath);
        StatusMessage = "Database restored. Restart the app to reload all data.";
        _dialogService.Info(StatusMessage);
    }

    private async Task CloudPlaceholderAsync()
    {
        _dialogService.Info(await _backupService.CloudBackupPlaceholderAsync());
    }
}
