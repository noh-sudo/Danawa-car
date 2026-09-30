using System.Windows;
using Danawa.Client.ViewModels;

namespace Danawa.Client.Views;

/// <summary>
/// Interaction logic for GeneralShellWindow.xaml
/// </summary>
public partial class GeneralShellWindow : Window
{
    public GeneralShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
