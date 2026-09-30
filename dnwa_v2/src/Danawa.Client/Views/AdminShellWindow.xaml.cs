using System.Windows;
using Danawa.Client.ViewModels;

namespace Danawa.Client.Views;

/// <summary>
/// Interaction logic for AdminShellWindow.xaml
/// </summary>
public partial class AdminShellWindow : Window
{
    public AdminShellWindow(AdminShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
