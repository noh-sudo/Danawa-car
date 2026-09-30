using System.IO;
using System.Text.Json;
using System.Windows;
using Danawa.Client.Services;
using Danawa.Client.ViewModels;
using Danawa.Client.Views;

namespace Danawa.Client;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServerConnection? _connection;
    private GeneralShellWindow? _generalShell;
    private AdminShellWindow? _adminShell;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        string settingsJson = await File.ReadAllTextAsync(settingsPath);
        using JsonDocument settings = JsonDocument.Parse(settingsJson);
        string host = settings.RootElement.GetProperty("ServerHost").GetString()!;
        int port = settings.RootElement.GetProperty("ServerPort").GetInt32();

        _connection = new ServerConnection();
        _connection.ConnectionChanged += OnConnectionChanged;

        try
        {
            await _connection.ConnectAsync(host, port);
        }
        catch
        {
            // 연결 실패는 ConnectionChanged 이벤트로 헤더 표시등에 반영, 셸은 메인 대신 서버 점검 화면(재접속 버튼)으로 시작 (COM-01)
        }

        var shellViewModel = new ShellViewModel(_connection);
        shellViewModel.AdminLoginSucceeded += OnAdminLoginSucceeded;

        _generalShell = new GeneralShellWindow(shellViewModel);
        MainWindow = _generalShell;
        _generalShell.Show();
    }

    private void OnAdminLoginSucceeded(string loginId, string role)
    {
        Dispatcher.Invoke(() =>
        {
            var adminViewModel = new AdminShellViewModel(_connection!, loginId);
            adminViewModel.LoggedOut += OnAdminLoggedOut;

            _adminShell = new AdminShellWindow(adminViewModel);
            MainWindow = _adminShell;
            _adminShell.Show();
            _generalShell?.Close();
            _generalShell = null;
        });
    }

    /// <summary>관리자 셸에서 연결이 끊기면 관리자 세션도 사라짐 → 일반회원 셸로 전환, 새 셸은 끊긴 상태로 생성되므로 서버 점검 화면부터 표시</summary>
    private void OnConnectionChanged(bool connected, string? message)
    {
        if (!connected && _adminShell is not null)
        {
            OnAdminLoggedOut();
        }
    }

    private void OnAdminLoggedOut()
    {
        Dispatcher.Invoke(() =>
        {
            var shellViewModel = new ShellViewModel(_connection!);
            shellViewModel.AdminLoginSucceeded += OnAdminLoginSucceeded;

            _generalShell = new GeneralShellWindow(shellViewModel);
            MainWindow = _generalShell;
            _generalShell.Show();
            _adminShell?.Close();
            _adminShell = null;
        });
    }
}
