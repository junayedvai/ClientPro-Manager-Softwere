using System.Windows.Input;
using ClientProManager.Helpers;
using ClientProManager.Services;

namespace ClientProManager.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private string _username = "admin";
    private string _password = "admin123";

    public LoginViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        LoginCommand = new AsyncRelayCommand(async _ => await LoginAsync());
    }

    public event EventHandler? LoginSucceeded;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public ICommand LoginCommand { get; }

    private async Task LoginAsync()
    {
        IsBusy = true;
        StatusMessage = "Signing in...";
        try
        {
            var result = await _authenticationService.LoginAsync(Username, Password);
            StatusMessage = result.Message;
            if (result.Success)
            {
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
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
}
