using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

/// <summary>car_list/car_search/car_detail.car/main_daily_top.car/main_favorite_top5가 공유하는 차량 요약 필드, docs/03_API명세.md 3절: car_price ~ car_price+option_total 범위로 화면 표시</summary>
public class CarSummaryDto
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

    [JsonPropertyName("fuel_min")]
    public double? FuelMin { get; set; }

    [JsonPropertyName("fuel_max")]
    public double? FuelMax { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }
}
