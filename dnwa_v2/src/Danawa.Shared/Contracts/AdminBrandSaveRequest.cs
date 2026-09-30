using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminBrandSaveRequest : RequestBase
{
    [JsonPropertyName("brand_id")]
    public int? BrandId { get; set; }

    [JsonPropertyName("brand_name")]
    public string BrandName { get; set; } = string.Empty;

    [JsonPropertyName("brand_icon")]
    public string? BrandIcon { get; set; }

    [JsonPropertyName("origin_type")]
    public string OriginType { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool? IsActive { get; set; }
}
