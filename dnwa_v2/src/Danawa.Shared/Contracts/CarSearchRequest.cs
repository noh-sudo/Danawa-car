using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class CarSearchRequest : RequestBase
{
    [JsonPropertyName("keyword")]
    public string Keyword { get; set; } = string.Empty;

    /// <summary>car_list와 같은 규칙. 8단계 추가.</summary>
    [JsonPropertyName("sort")]
    public string? Sort { get; set; }
}
