using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class BrandListResponse : ResponseBase
{
    [JsonPropertyName("brands")]
    public List<BrandDto> Brands { get; set; } = new();
}
