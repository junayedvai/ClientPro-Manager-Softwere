using System.Collections.ObjectModel;
using System.Windows.Input;
using ClientProManager.Helpers;
using ClientProManager.Models;
using ClientProManager.Services;

namespace ClientProManager.ViewModels;

public class UsersViewModel : ViewModelBase
{
    private readonly IUserService _userService;
    private readonly IActivityLogService _activityLogService;
    private readonly IDialogService _dialogService;
    private User? _selectedUser;
    private User _editableUser = NewUser();
    private string _newPassword = "admin123";

    public UsersViewModel(IUserService userService, IActivityLogService activityLogService, IDialogService dialogService)
    {
        _userService = userService;
        _activityLogService = activityLogService;
        _dialogService = dialogService;
        LoadCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        NewCommand = new RelayCommand(_ =>
        {
            EditableUser = NewUser();
            NewPassword = "admin123";
        });
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync());
        DisableCommand = new AsyncRelayCommand(async _ => await DisableAsync());
        ResetPasswordCommand = new AsyncRelayCommand(async _ => await ResetPasswordAsync());
        _ = LoadAsync();
    }

    public ObservableCollection<User> Users { get; } = [];
    public ObservableCollection<ActivityLog> ActivityLogs { get; } = [];
    public string[] Roles { get; } = UserRoles.All;

    public User? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value) && value is not null)
            {
                EditableUser = Clone(value);
            }
        }
    }

    public User EditableUser
    {
        get => _editableUser;
        set => SetProperty(ref _editableUser, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public ICommand LoadCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DisableCommand { get; }
    public ICommand ResetPasswordCommand { get; }

    private async Task LoadAsync()
    {
        Users.ReplaceWith(await _userService.GetUsersAsync());
        ActivityLogs.ReplaceWith(await _activityLogService.GetRecentAsync());
        StatusMessage = $"{Users.Count} user(s) loaded.";
    }

    private async Task SaveAsync()
    {
        try
        {
            await _userService.SaveUserAsync(EditableUser, EditableUser.Id == 0 ? NewPassword : null);
            await LoadAsync();
            StatusMessage = "User saved.";
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    private async Task DisableAsync()
    {
        if (SelectedUser is null)
        {
            return;
        }

        if (!_dialogService.Confirm($"Disable user {SelectedUser.Username}?"))
        {
            return;
        }

        await _userService.DisableUserAsync(SelectedUser.Id);
        await LoadAsync();
    }

    private async Task ResetPasswordAsync()
    {
        if (SelectedUser is null)
        {
            return;
        }

        try
        {
            await _userService.ResetPasswordAsync(SelectedUser.Id, NewPassword);
            await LoadAsync();
            StatusMessage = "Password reset.";
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    private static User NewUser() => new()
    {
        Role = UserRoles.Staff,
        IsActive = true
    };

    private static User Clone(User source) => new()
    {
        Id = source.Id,
        FullName = source.FullName,
        Username = source.Username,
        Email = source.Email,
        Phone = source.Phone,
        PasswordHash = source.PasswordHash,
        Role = source.Role,
        IsActive = source.IsActive,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
        LastLoginAt = source.LastLoginAt,
        MustChangePassword = source.MustChangePassword
    };
}
