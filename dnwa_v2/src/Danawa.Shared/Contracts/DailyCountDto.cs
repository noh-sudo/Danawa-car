using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class DailyCountDto
{
    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }
}
