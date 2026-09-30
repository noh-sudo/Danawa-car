using System.Collections.ObjectModel;
using System.Windows.Input;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>11 관리자 — 통계, 재조회 시 이전 결과를 먼저 비움 (ADM-09)</summary>
public sealed class AdminStatsViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;

    private string _dateFrom = DateTime.Today.AddDays(-6).ToString("yyyy-MM-dd");
    public string DateFrom { get => _dateFrom; set => SetProperty(ref _dateFrom, value); }

    private string _dateTo = DateTime.Today.ToString("yyyy-MM-dd");
    public string DateTo { get => _dateTo; set => SetProperty(ref _dateTo, value); }

    private string? _error;
    public string? Error { get => _error; set => SetProperty(ref _error, value); }

    public ObservableCollection<DailyStatPointViewModel> DailyPoints { get; } = new();
    public ObservableCollection<WeeklyRankRow> WeeklyDomestic { get; } = new();
    public ObservableCollection<WeeklyRankRow> WeeklyImported { get; } = new();

    public ICommand SearchCommand { get; }

    public AdminStatsViewModel(ServerConnection connection)
    {
        _connection = connection;
        SearchCommand = new RelayCommand(async _ => await SearchDailyAsync(), _ => !IsBusy);
        _ = SearchDailyAsync();
        _ = LoadWeeklyAsync();
    }

    private async Task SearchDailyAsync()
    {
        if (IsBusy)
        {
            return;
        }

        Error = null;

        if (!DateOnly.TryParse(DateFrom, out DateOnly from) || !DateOnly.TryParse(DateTo, out DateOnly to))
        {
            Error = "날짜 형식이 올바르지 않습니다 (YYYY-MM-DD).";
            return;
        }

        if (from > to)
        {
            Error = "시작일이 종료일보다 늦을 수 없습니다.";
            return;
        }

        // ADM-09: 재조회 시 이전 결과를 먼저 비움
        DailyPoints.Clear();

        IsBusy = true;
        try
        {
            var resp = await _connection.SendAsync<AdminStatsDailyResponse>(new AdminStatsDailyRequest
            {
                Type = RequestType.AdminStatsDaily,
                DateFrom = DateFrom,
                DateTo = DateTo
            });

            if (resp.Result != "OK")
            {
                Error = resp.Message;
                return;
            }

            Dictionary<string, int> countByDay = resp.Days.ToDictionary(d => d.Day, d => d.Count);
            int maxCount = resp.Days.Count == 0 ? 0 : resp.Days.Max(d => d.Count);

            for (DateOnly day = from; day <= to; day = day.AddDays(1))
            {
                string key = day.ToString("yyyy-MM-dd");
                int count = countByDay.GetValueOrDefault(key, 0);
                DailyPoints.Add(new DailyStatPointViewModel(key, count, maxCount));
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

    private async Task LoadWeeklyAsync()
    {
        try
        {
            var resp = await _connection.SendAsync<AdminStatsWeeklyTop5Response>(
                new RequestBase { Type = RequestType.AdminStatsWeeklyTop5 });

            WeeklyDomestic.Clear();
            foreach (WeeklyRankRow row in WeeklyRankRow.From(resp.Domestic, 0))
            {
                WeeklyDomestic.Add(row);
            }

            WeeklyImported.Clear();
            foreach (WeeklyRankRow row in WeeklyRankRow.From(resp.Imported, 0))
            {
                WeeklyImported.Add(row);
            }
        }
        catch
        {
            // 연결 끊김 등: 빈 목록 유지
        }
    }
}
