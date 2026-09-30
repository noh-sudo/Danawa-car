using System.Windows.Input;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>일반회원 셸의 상태, 공통 헤더(연결 상태, 로그인 정보)와 콘텐츠 영역 전환 담당</summary>
public sealed class ShellViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;
    private ViewModelBase _currentViewModel;
    private ViewModelBase? _returnTarget;
    private bool _isConnected;
    private string? _connectionMessage;
    private string? _loginId;

    public event Action<string, string>? AdminLoginSucceeded; // (loginId, role)

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            // 연결이 끊긴 동안에는 점검 화면에서 다른 화면으로 이동 안 함 (헤더·GNB 클릭 포함)
            if (!IsConnected && value is not ServerDownViewModel)
            {
                return;
            }

            SetProperty(ref _currentViewModel, value);
        }
    }

    public bool IsConnected
    {
        get => _isConnected;
        private set => SetProperty(ref _isConnected, value);
    }

    public string? ConnectionMessage
    {
        get => _connectionMessage;
        private set => SetProperty(ref _connectionMessage, value);
    }

    public string? LoginId
    {
        get => _loginId;
        private set
        {
            if (SetProperty(ref _loginId, value))
            {
                OnPropertyChanged(nameof(IsLoggedIn));
            }
        }
    }

    public bool IsLoggedIn => _loginId is not null;

    private string _searchKeyword = string.Empty;
    public string SearchKeyword
    {
        get => _searchKeyword;
        set => SetProperty(ref _searchKeyword, value);
    }

    // GNB 밑줄 탭. 마지막으로 고른 탭을 유지한다 (와이어프레임 05: 국산차에서 들어온 상세는 국산차 탭이 선택된 채).
    private string _activeTab = "ALL";
    public string ActiveTab
    {
        get => _activeTab;
        private set
        {
            if (SetProperty(ref _activeTab, value))
            {
                OnPropertyChanged(nameof(IsAllTab));
                OnPropertyChanged(nameof(IsDomesticTab));
                OnPropertyChanged(nameof(IsImportTab));
                OnPropertyChanged(nameof(ActiveTabLabel));
            }
        }
    }

    public bool IsAllTab => ActiveTab == "ALL";
    public bool IsDomesticTab => ActiveTab == "DOMESTIC";
    public bool IsImportTab => ActiveTab == "IMPORT";

    /// <summary>05 상세의 경로 표시 첫 칸 ("전체차량 › 현대 › 아반떼").</summary>
    public string ActiveTabLabel => ActiveTab switch
    {
        "DOMESTIC" => "국산차",
        "IMPORT" => "수입차",
        _ => "전체차량"
    };

    public ICommand NavigateHomeCommand { get; }
    public ICommand NavigateToLoginCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand ShowAllCarsCommand { get; }
    public ICommand ShowDomesticCarsCommand { get; }
    public ICommand ShowImportedCarsCommand { get; }
    public ICommand NavigateToFavoritesCommand { get; }
    public ICommand NavigateToQuotesCommand { get; }

    public ShellViewModel(ServerConnection connection)
    {
        _connection = connection;
        _connection.ConnectionChanged += OnConnectionChanged;
        _isConnected = connection.IsConnected;
        _connectionMessage = connection.LastConnectionMessage;

        NavigateHomeCommand = new RelayCommand(_ => NavigateHome());
        NavigateToLoginCommand = new RelayCommand(_ => NavigateToLogin(null));
        LogoutCommand = new RelayCommand(async _ => await LogoutAsync(), _ => IsLoggedIn);
        SearchCommand = new RelayCommand(_ => OnSearch());
        ClearSearchCommand = new RelayCommand(_ => SearchKeyword = string.Empty);
        ShowAllCarsCommand = new RelayCommand(_ => ShowTab("ALL", () => CarListViewModel.ForAll(_connection, this)));
        ShowDomesticCarsCommand = new RelayCommand(_ =>
            ShowTab("DOMESTIC", () => CarListViewModel.ForOrigin(_connection, this, "DOMESTIC", "국산차")));
        ShowImportedCarsCommand = new RelayCommand(_ =>
            ShowTab("IMPORT", () => CarListViewModel.ForOrigin(_connection, this, "IMPORT", "수입차")));
        NavigateToFavoritesCommand = new RelayCommand(_ => NavigateToFavorites());
        NavigateToQuotesCommand = new RelayCommand(_ => NavigateToQuotes());

        // 실행 시 접속 실패했으면 메인 대신 점검 화면부터 표시 (COM-01)
        _currentViewModel = _isConnected
            ? new MainViewModel(_connection, this)
            : new ServerDownViewModel(_connection);
    }

    private void NavigateToFavorites()
    {
        if (!IsLoggedIn)
        {
            RequireLogin(CurrentViewModel);
            return;
        }

        NavigateTo(new FavoriteListViewModel(_connection, this));
    }

    private void NavigateToQuotes()
    {
        if (!IsLoggedIn)
        {
            RequireLogin(CurrentViewModel);
            return;
        }

        NavigateTo(new QuoteListViewModel(_connection));
    }

    private void OnSearch()
    {
        string keyword = SearchKeyword.Trim();
        if (keyword.Length == 0 || !IsConnected)
        {
            return;
        }

        NavigateTo(CarListViewModel.ForSearch(_connection, this, keyword));
    }

    /// <summary>GNB 탭 클릭, 연결이 끊긴 동안에는 점검 화면에 머물러야 하므로 탭 밑줄도 그대로(목록 VM은 생성 즉시 서버를 부르므로 연결됐을 때만 생성)</summary>
    private void ShowTab(string tab, Func<CarListViewModel> createList)
    {
        if (!IsConnected)
        {
            return;
        }

        ActiveTab = tab;
        NavigateTo(createList());
    }

    /// <summary>04 목록·05 상세 등 화면 사이를 자유롭게 이동할 때 사용</summary>
    public void NavigateTo(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
    }

    /// <summary>로그인이 필요한 동작을 비로그인 상태에서 시도했을 때 호출 — 로그인 성공 후 호출 시점의 화면(returning)으로 복귀 (USR-09)</summary>
    public void RequireLogin(ViewModelBase returning)
    {
        NavigateToLogin(returning);
    }

    public void NavigateToSignup()
    {
        CurrentViewModel = new SignupViewModel(_connection, this);
    }

    public void NavigateHome()
    {
        if (!IsConnected)
        {
            return;
        }

        _returnTarget = null;
        ActiveTab = "ALL";
        CurrentViewModel = new MainViewModel(_connection, this);
    }

    private void NavigateToLogin(ViewModelBase? returning)
    {
        _returnTarget = returning ?? (CurrentViewModel is LoginViewModel or SignupViewModel ? null : CurrentViewModel);
        CurrentViewModel = new LoginViewModel(_connection, this);
    }

    public void OnSignupSucceeded()
    {
        NavigateToLogin(_returnTarget);
    }

    public void OnLoginSucceeded(string loginId, string role)
    {
        LoginId = loginId;

        if (role == "ADMIN")
        {
            AdminLoginSucceeded?.Invoke(loginId, role);
            return;
        }

        ViewModelBase? returning = _returnTarget;
        _returnTarget = null;
        CurrentViewModel = returning ?? new MainViewModel(_connection, this);
        returning?.OnReturnedAfterLogin();
        RelayCommand.RaiseCanExecuteChanged();
    }

    private async Task LogoutAsync()
    {
        await _connection.SendAsync(new RequestBase { Type = RequestType.Logout });
        LoginId = null;
        NavigateHome();
        RelayCommand.RaiseCanExecuteChanged();
    }

    private void OnConnectionChanged(bool connected, string? message)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            IsConnected = connected;
            ConnectionMessage = message;

            if (!connected)
            {
                // 서버 세션은 연결에 붙어 있음 — 끊기면 로그인 상태도 사라짐
                LoginId = null;
                _returnTarget = null;
                if (CurrentViewModel is not ServerDownViewModel)
                {
                    CurrentViewModel = new ServerDownViewModel(_connection);
                }
            }
            else if (CurrentViewModel is ServerDownViewModel)
            {
                // 재접속 성공: 새 세션이므로 비로그인 상태로 메인 이동
                NavigateHome();
            }

            RelayCommand.RaiseCanExecuteChanged();
        });
    }
}
