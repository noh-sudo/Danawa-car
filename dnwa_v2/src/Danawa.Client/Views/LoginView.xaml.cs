using System.Windows.Controls;
using Danawa.Client.ViewModels;

namespace Danawa.Client.Views;

/// <summary>
/// Interaction logic for LoginView.xaml
/// </summary>
public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    // PasswordBox.Password는 보안상 바인딩 불가 DependencyProperty가 아님 — 코드비하인드에서 옮김
    private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.Password = PasswordBox.Password;
        }
    }
}
