using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class ImageGetRequest : RequestBase
{
    [JsonPropertyName("image_url")]
    public string ImageUrl { get; set; } = string.Empty;
}
