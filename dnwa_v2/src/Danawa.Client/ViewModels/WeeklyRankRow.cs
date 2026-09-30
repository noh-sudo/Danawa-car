using Danawa.Shared.Contracts;

namespace Danawa.Client.ViewModels;

/// <summary>08/11 화면 주간 견적 TOP5 표의 한 줄, 빈 순위는 "(없음)"으로 채움 (와이어프레임 08)</summary>
public sealed record WeeklyRankRow(int Rank, string CarName, string BrandName, string CountText, bool IsEmpty)
{
    public static List<WeeklyRankRow> From(IEnumerable<WeeklyTopCarDto> cars, int padTo)
    {
        var rows = cars.Select((c, i) => new WeeklyRankRow(i + 1, c.CarName, c.BrandName, $"{c.QuoteCount}건", false)).ToList();
        while (rows.Count < padTo)
        {
            rows.Add(new WeeklyRankRow(rows.Count + 1, "(없음)", "-", "0건", true));
        }

        return rows;
    }
}
