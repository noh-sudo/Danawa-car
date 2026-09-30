using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class FavoriteTopCarDto : CarSummaryDto
{
    [JsonPropertyName("favorite_count")]
    public int FavoriteCount { get; set; }
}
