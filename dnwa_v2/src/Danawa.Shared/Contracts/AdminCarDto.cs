using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminCarDto
{
    [JsonPropertyName("car_id")]
    public int CarId { get; set; }

    [JsonPropertyName("brand_id")]
    public int BrandId { get; set; }

    [JsonPropertyName("brand_name")]
    public string BrandName { get; set; } = string.Empty;

    [JsonPropertyName("car_name")]
    public string CarName { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("car_price")]
    public int CarPrice { get; set; }

    [JsonPropertyName("fuel_min")]
    public double? FuelMin { get; set; }

    [JsonPropertyName("fuel_max")]
    public double? FuelMax { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
}
