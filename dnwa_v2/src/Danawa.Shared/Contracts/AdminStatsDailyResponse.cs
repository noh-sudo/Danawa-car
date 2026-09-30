using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminStatsDailyResponse : ResponseBase
{
    [JsonPropertyName("days")]
    public List<DailyCountDto> Days { get; set; } = new();
}
