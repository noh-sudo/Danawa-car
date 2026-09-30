using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class DailyTopSummaryDto
{
    [JsonPropertyName("car_id")]
    public int CarId { get; set; }

    [JsonPropertyName("car_name")]
    public string CarName { get; set; } = string.Empty;

    /// <summary>8단계 추가.</summary>
    [JsonPropertyName("brand_name")]
    public string BrandName { get; set; } = string.Empty;

    [JsonPropertyName("quote_count")]
    public int QuoteCount { get; set; }
}
