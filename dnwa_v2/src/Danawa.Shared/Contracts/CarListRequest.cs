using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class CarListRequest : RequestBase
{
    [JsonPropertyName("brand_id")]
    public int? BrandId { get; set; }

    [JsonPropertyName("origin_type")]
    public string? OriginType { get; set; }

    /// <summary>"POPULAR" / "PRICE_ASC" / 생략(car_id순). 8단계 추가.</summary>
    [JsonPropertyName("sort")]
    public string? Sort { get; set; }
}
