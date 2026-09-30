using System.Text.RegularExpressions;
using System.Windows.Input;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

public sealed partial class SignupViewModel : ViewModelBase
{
    [GeneratedRegex("^[A-Za-z0-9]{4,30}$")]
    private static partial Regex LoginIdPattern();

    private readonly ServerConnection _connection;
    private readonly ShellViewModel _shell;

    private string _loginId = string.Empty;
    private string _password = string.Empty;
    private string _passwordConfirm = string.Empty;
    private string? _idCheckMessage;
    private bool _idAvailable;
    private string? _errorMessage;

    public string LoginId
    {
        get => _loginId;
        set
        {
            if (SetProperty(ref _loginId, value))
            {
                _idAvailable = false;
                IdCheckMessage = null;
            }
        }
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string PasswordConfirm
    {
        get => _passwordConfirm;
        set => SetProperty(ref _passwordConfirm, value);
    }

    public string? IdCheckMessage
    {
        get => _idCheckMessage;
        set
        {
            if (SetProperty(ref _idCheckMessage, value))
            {
                OnPropertyChanged(nameof(IsIdAvailable));
            }
        }
    }

    /// <summary>중복확인 메시지 색(사용 가능=초록, 그 외=빨강).</summary>
    public bool IsIdAvailable => _idAvailable;

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }


    public ICommand CheckIdCommand { get; }
    public ICommand SignupCommand { get; }
    public ICommand GoToLoginCommand { get; }

    public SignupViewModel(ServerConnection connection, ShellViewModel shell)
    {
        _connection = connection;
        _shell = shell;
        CheckIdCommand = new RelayCommand(async _ => await CheckIdAsync(), _ => !IsBusy);
        SignupCommand = new RelayCommand(async _ => await SignupAsync(), _ => !IsBusy);
        GoToLoginCommand = new RelayCommand(_ => _shell.OnSignupSucceeded());
    }

    private async Task CheckIdAsync()
    {
        if (!LoginIdPattern().IsMatch(LoginId))
        {
            IdCheckMessage = "아이디는 4~30자 영문/숫자만 가능합니다.";
            return;
        }

        IsBusy = true;
        try
        {
            var resp = await _connection.SendAsync<CheckIdResponse>(
                new CheckIdRequest { Type = RequestType.CheckId, LoginId = LoginId });

            _idAvailable = resp.Result == "OK" && resp.Available;
            IdCheckMessage = _idAvailable ? "사용 가능한 아이디입니다." : "이미 사용 중인 아이디입니다.";
        }
        catch (Exception ex)
        {
            IdCheckMessage = $"확인 실패: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SignupAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;

        if (!LoginIdPattern().IsMatch(LoginId))
        {
            ErrorMessage = "아이디는 4~30자 영문/숫자만 가능합니다.";
            return;
        }

        if (Password.Length < 8)
        {
            ErrorMessage = "비밀번호는 8자 이상이어야 합니다.";
            return;
        }

        if (Password != PasswordConfirm)
        {
            ErrorMessage = "비밀번호가 서로 일치하지 않습니다.";
            return;
        }

        IsBusy = true;
        try
        {
            var resp = await _connection.SendAsync(
                new SignupRequest { Type = RequestType.Signup, LoginId = LoginId, Password = Password });

            if (resp.Result != "OK")
            {
                // 아이디 중복 확인과 가입 사이에 남이 먼저 가입했을 수 있다 (CONFLICT) — 최종 판정은 서버의 UNIQUE 제약.
                ErrorMessage = resp.Message;
                return;
            }

            _shell.OnSignupSucceeded();
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
