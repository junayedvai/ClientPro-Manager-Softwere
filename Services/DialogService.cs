using System.Windows;

namespace ClientProManager.Services;

public interface IDialogService
{
    void Info(string message, string title = "ClientPro Manager");
    void Error(string message, string title = "ClientPro Manager");
    bool Confirm(string message, string title = "Confirm");
}

public class DialogService : IDialogService
{
    public void Info(string message, string title = "ClientPro Manager")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void Error(string message, string title = "ClientPro Manager")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool Confirm(string message, string title = "Confirm")
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}
