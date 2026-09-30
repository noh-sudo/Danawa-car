using System.Windows.Input;
using Danawa.Client.Services;

namespace Danawa.Client.ViewModels;

/// <summary>서버 점검 화면 (COM-01, COM-08), 실행 시 접속 실패 또는 사용 중 연결 끊김 때 콘텐츠 영역에 표시 — [재접속] 성공 시 ServerConnection이 ConnectionChanged(true)를 올리고, ShellViewModel이 메인으로 전환</summary>
public sealed class ServerDownViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    private string _lastAttemptText;
    public string LastAttemptText
    {
        get => _lastAttemptText;
        private set => SetProperty(ref _lastAttemptText, value);
    }

    public ICommand ReconnectCommand { get; }

    public ServerDownViewModel(ServerConnection connection)
    {
        _connection = connection;
        _lastAttemptText = $"연결 끊김 {DateTime.Now:HH:mm:ss}";
        ReconnectCommand = new RelayCommand(async _ => await ReconnectAsync(), _ => !IsBusy);
    }

    private async Task ReconnectAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;
        IsBusy = true;
        try
        {
            await _connection.ReconnectAsync();
        }
        catch
        {
            // 예외 원문("대상 컴퓨터에서 연결을 거부했으므로…")은 보여주지 않고 안내 문구만 띄운 채 점검 화면에 유지
            ErrorMessage = "점검이 끝난 후 다시 시도해주세요";
            LastAttemptText = $"마지막 시도 {DateTime.Now:HH:mm:ss}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
