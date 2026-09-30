using System.Collections.ObjectModel;
using Danawa.Client.Services;
using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>08 관리자 대시보드. 이 화면의 TOP5는 견적 기준이다 (메인 화면은 관심 기준).</summary>
public sealed class AdminDashboardViewModel : ViewModelBase
{
    private readonly ServerConnection _connection;

    private int _totalQuotes;
    public int TotalQuotes { get => _totalQuotes; private set => SetProperty(ref _totalQuotes, value); }

    private int _todayQuotes;
    public int TodayQuotes { get => _todayQuotes; private set => SetProperty(ref _todayQuotes, value); }

    private DailyTopSummaryDto? _dailyTop;
    public DailyTopSummaryDto? DailyTop
    {
        get => _dailyTop;
        private set
        {
            if (SetProperty(ref _dailyTop, value))
            {
                OnPropertyChanged(nameof(HasDailyTop));
            }
        }
    }

    public bool HasDailyTop => DailyTop is not null;

    public ObservableCollection<WeeklyRankRow> WeeklyDomestic { get; } = new();
    public ObservableCollection<WeeklyRankRow> WeeklyImported { get; } = new();

    private string _asOfText = string.Empty;
    public string AsOfText { get => _asOfText; private set => SetProperty(ref _asOfText, value); }

    public AdminDashboardViewModel(ServerConnection connection)
    {
        _connection = connection;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            // admin_stats_summary(IF-ADM-009)는 오늘 "전체" 견적 건수를 안 주므로, 이미 공개된 main_summary(IF-CAR-008)를 같이 호출해 today_quotes 확보 — ADMIN 세션도 호출 가능
            var summaryTask = _connection.SendAsync<AdminStatsSummaryResponse>(
                new RequestBase { Type = RequestType.AdminStatsSummary });
            var mainSummaryTask = _connection.SendAsync<MainSummaryResponse>(
                new RequestBase { Type = RequestType.MainSummary });
            var weeklyTask = _connection.SendAsync<AdminStatsWeeklyTop5Response>(
                new RequestBase { Type = RequestType.AdminStatsWeeklyTop5 });

            await Task.WhenAll(summaryTask, mainSummaryTask, weeklyTask);

            TotalQuotes = summaryTask.Result.TotalQuotes;
            DailyTop = summaryTask.Result.DailyTop;
            TodayQuotes = mainSummaryTask.Result.TodayQuotes;

            // 빈 순위는 "(없음)"으로 채워 항상 5줄 (와이어프레임 08)
            WeeklyDomestic.Clear();
            foreach (WeeklyRankRow row in WeeklyRankRow.From(weeklyTask.Result.Domestic, 5))
            {
                WeeklyDomestic.Add(row);
            }

            WeeklyImported.Clear();
            foreach (WeeklyRankRow row in WeeklyRankRow.From(weeklyTask.Result.Imported, 5))
            {
                WeeklyImported.Add(row);
            }

            AsOfText = $"{DateTime.Now:yyyy-MM-dd HH:mm} 기준";
        }
        catch
        {
            // 연결 끊김 등: 빈 화면 유지
        }
        finally
        {
            IsBusy = false;
        }
    }
}
