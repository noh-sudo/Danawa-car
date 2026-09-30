using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class MainSummaryResponse : ResponseBase
{
    [JsonPropertyName("today_quotes")]
    public int TodayQuotes { get; set; }

    [JsonPropertyName("total_quotes")]
    public int TotalQuotes { get; set; }

    [JsonPropertyName("my_quotes")]
    public int MyQuotes { get; set; }

    [JsonPropertyName("my_favorites")]
    public int MyFavorites { get; set; }
}
