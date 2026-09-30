using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class ImageGetResponse : ResponseBase
{
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;
}
