using System.Windows;
using ClientProManager.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClientProManager.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.LoginSucceeded += (_, _) =>
        {
            var mainWindow = App.AppHost.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            Close();
        };
    }
}
