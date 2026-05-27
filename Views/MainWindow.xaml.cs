using System.Windows;
using ClientProManager.ViewModels;

namespace ClientProManager.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
