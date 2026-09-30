namespace Danawa.Client.ViewModels;

/// <summary>11 통계 화면의 세로 막대 1개, 서버는 0건인 날을 응답에서 제외 — 여기서 0으로 채워서 그림</summary>
public sealed class DailyStatPointViewModel
{
    private const double MaxBarHeight = 150;

    public string Day { get; }
    public int Count { get; }
    public double BarHeight { get; }

    /// <summary>"2026-09-16" → "09-16" (막대 아래 라벨)</summary>
    public string DayLabel => Day.Length >= 10 ? Day[5..] : Day;

    public DailyStatPointViewModel(string day, int count, int maxCount)
    {
        Day = day;
        Count = count;
        BarHeight = maxCount == 0 ? 0 : (double)count / maxCount * MaxBarHeight;
    }
}
