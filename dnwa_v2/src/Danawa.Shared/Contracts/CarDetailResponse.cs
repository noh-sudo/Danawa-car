using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class CarDetailResponse : ResponseBase
{
    [JsonPropertyName("car")]
    public CarSummaryDto? Car { get; set; }

    [JsonPropertyName("options")]
    public List<CarOptionDto> Options { get; set; } = new();

    [JsonPropertyName("is_favorite")]
    public bool IsFavorite { get; set; }
}
