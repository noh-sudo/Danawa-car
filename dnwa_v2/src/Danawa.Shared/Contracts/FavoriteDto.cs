using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class FavoriteDto
{
    [JsonPropertyName("car_id")]
    public int CarId { get; set; }

    [JsonPropertyName("car_name")]
    public string CarName { get; set; } = string.Empty;

    [JsonPropertyName("brand_name")]
    public string BrandName { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("car_price")]
    public int CarPrice { get; set; }

    [JsonPropertyName("option_total")]
    public long OptionTotal { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}
