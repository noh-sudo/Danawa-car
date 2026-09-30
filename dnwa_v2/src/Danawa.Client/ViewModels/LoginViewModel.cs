using System.Windows.Input;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;
    private readonly ShellViewModel _shell;
    private string _loginId = string.Empty;
    private string _password = string.Empty;
    private string? _errorMessage;

    public string LoginId
    {
        get => _loginId;
        set => SetProperty(ref _loginId, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }


    public ICommand LoginCommand { get; }
    public ICommand GoToSignupCommand { get; }

    public LoginViewModel(ServerConnection connection, ShellViewModel shell)
    {
        _connection = connection;
        _shell = shell;
        LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => !IsBusy);
        GoToSignupCommand = new RelayCommand(_ => _shell.NavigateToSignup());
    }

    private async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var request = new LoginRequest { Type = RequestType.Login, LoginId = LoginId, Password = Password };
            LoginResponse resp = await _connection.SendAsync<LoginResponse>(request);

            if (resp.Result != "OK")
            {
                ErrorMessage = resp.Message;
                return;
            }

            _shell.OnLoginSucceeded(resp.LoginId, resp.Role);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"서버와 통신할 수 없습니다: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
