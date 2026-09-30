using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminLogListRequest : RequestBase
{
    [JsonPropertyName("date_from")]
    public string? DateFrom { get; set; }

    [JsonPropertyName("date_to")]
    public string? DateTo { get; set; }
}
