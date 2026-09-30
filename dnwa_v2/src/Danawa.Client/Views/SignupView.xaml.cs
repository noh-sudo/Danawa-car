using System.Windows.Controls;
using Danawa.Client.ViewModels;

namespace Danawa.Client.Views;

/// <summary>
/// Interaction logic for SignupView.xaml
/// </summary>
public partial class SignupView : UserControl
{
    public SignupView()
    {
        InitializeComponent();
    }

    private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SignupViewModel vm)
        {
            vm.Password = PasswordBox.Password;
        }
    }

    private void PasswordConfirmBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SignupViewModel vm)
        {
            vm.PasswordConfirm = PasswordConfirmBox.Password;
        }
    }
}
