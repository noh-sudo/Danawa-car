using System.Windows.Input;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>관리자 셸, 화면 08~12(대시보드/차량·브랜드/옵션/통계/로그) 사이 전환</summary>
public sealed class AdminShellViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;

    public string LoginId { get; }

    private ViewModelBase _currentViewModel;
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (SetProperty(ref _currentViewModel, value))
            {
                OnPropertyChanged(nameof(IsDashboardActive));
                OnPropertyChanged(nameof(IsCarBrandActive));
                OnPropertyChanged(nameof(IsOptionActive));
                OnPropertyChanged(nameof(IsStatsActive));
                OnPropertyChanged(nameof(IsLogActive));
            }
        }
    }

    // 사이드바에서 현재 메뉴 강조 (와이어프레임 08~12)
    public bool IsDashboardActive => CurrentViewModel is AdminDashboardViewModel;
    public bool IsCarBrandActive => CurrentViewModel is AdminCarBrandViewModel;
    public bool IsOptionActive => CurrentViewModel is AdminOptionViewModel;
    public bool IsStatsActive => CurrentViewModel is AdminStatsViewModel;
    public bool IsLogActive => CurrentViewModel is AdminLogViewModel;

    public ICommand LogoutCommand { get; }
    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowCarBrandCommand { get; }
    public ICommand ShowOptionCommand { get; }
    public ICommand ShowStatsCommand { get; }
    public ICommand ShowLogCommand { get; }

    public event Action? LoggedOut;

    public AdminShellViewModel(ServerConnection connection, string loginId)
    {
        _connection = connection;
        LoginId = loginId;
        LogoutCommand = new RelayCommand(async _ => await LogoutAsync());
        ShowDashboardCommand = new RelayCommand(_ => CurrentViewModel = new AdminDashboardViewModel(_connection));
        ShowCarBrandCommand = new RelayCommand(_ => CurrentViewModel = new AdminCarBrandViewModel(_connection));
        ShowOptionCommand = new RelayCommand(_ => CurrentViewModel = new AdminOptionViewModel(_connection));
        ShowStatsCommand = new RelayCommand(_ => CurrentViewModel = new AdminStatsViewModel(_connection));
        ShowLogCommand = new RelayCommand(_ => CurrentViewModel = new AdminLogViewModel(_connection));

        _currentViewModel = new AdminDashboardViewModel(_connection);
    }

    private async Task LogoutAsync()
    {
        await _connection.SendAsync(new RequestBase { Type = RequestType.Logout });
        LoggedOut?.Invoke();
    }
}
