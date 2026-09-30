using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class QuoteDto
{
    [JsonPropertyName("quote_id")]
    public int QuoteId { get; set; }

    [JsonPropertyName("car_id")]
    public int CarId { get; set; }

    [JsonPropertyName("car_name")]
    public string CarName { get; set; } = string.Empty;

    /// <summary>현재 값(스냅샷 아님). 8단계 추가.</summary>
    [JsonPropertyName("brand_name")]
    public string BrandName { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("car_price")]
    public int CarPrice { get; set; }

    [JsonPropertyName("total_price")]
    public int TotalPrice { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public List<QuoteOptionDto> Options { get; set; } = new();
}
