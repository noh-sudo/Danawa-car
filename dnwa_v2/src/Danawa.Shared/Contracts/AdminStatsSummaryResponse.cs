using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminStatsSummaryResponse : ResponseBase
{
    [JsonPropertyName("total_quotes")]
    public int TotalQuotes { get; set; }

    [JsonPropertyName("daily_top")]
    public DailyTopSummaryDto? DailyTop { get; set; }
}
