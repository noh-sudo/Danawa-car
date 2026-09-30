using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminStatsDailyRequest : RequestBase
{
    [JsonPropertyName("date_from")]
    public string DateFrom { get; set; } = string.Empty;

    [JsonPropertyName("date_to")]
    public string DateTo { get; set; } = string.Empty;
}
