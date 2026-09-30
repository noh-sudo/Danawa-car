using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class RequestBase
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("req_id")]
    public int ReqId { get; set; }
}
