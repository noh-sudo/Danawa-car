using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminBrandListResponse : ResponseBase
{
    [JsonPropertyName("brands")]
    public List<AdminBrandDto> Brands { get; set; } = new();
}
