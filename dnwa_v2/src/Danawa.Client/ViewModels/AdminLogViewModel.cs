using System.Collections.ObjectModel;
using System.Windows.Input;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>12 관리자 — 접속 로그 조회. 기간을 생략하면 최근 200건.</summary>
public sealed class AdminLogViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;

    private string _dateFrom = string.Empty;
    public string DateFrom { get => _dateFrom; set => SetProperty(ref _dateFrom, value); }

    private string _dateTo = string.Empty;
    public string DateTo { get => _dateTo; set => SetProperty(ref _dateTo, value); }

    private string? _error;
    public string? Error { get => _error; set => SetProperty(ref _error, value); }

    public ObservableCollection<AdminLogDto> Logs { get; } = new();

    public ICommand SearchCommand { get; }

    public AdminLogViewModel(ServerConnection connection)
    {
        _connection = connection;
        SearchCommand = new RelayCommand(async _ => await SearchAsync(), _ => !IsBusy);
        _ = SearchAsync();
    }

    private async Task SearchAsync()
    {
        if (IsBusy)
        {
            return;
        }

        Error = null;
        Logs.Clear();

        IsBusy = true;
        try
        {
            var resp = await _connection.SendAsync<AdminLogListResponse>(new AdminLogListRequest
            {
                Type = RequestType.AdminLogList,
                DateFrom = string.IsNullOrWhiteSpace(DateFrom) ? null : DateFrom,
                DateTo = string.IsNullOrWhiteSpace(DateTo) ? null : DateTo
            });

            if (resp.Result != "OK")
            {
                Error = resp.Message;
                return;
            }

            foreach (AdminLogDto log in resp.Logs)
            {
                Logs.Add(log);
            }
        }
        catch (Exception ex)
        {
            Error = $"서버와 통신할 수 없습니다: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
