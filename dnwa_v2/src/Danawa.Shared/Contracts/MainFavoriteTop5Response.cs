using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class MainFavoriteTop5Response : ResponseBase
{
    [JsonPropertyName("domestic")]
    public List<FavoriteTopCarDto> Domestic { get; set; } = new();

    [JsonPropertyName("imported")]
    public List<FavoriteTopCarDto> Imported { get; set; } = new();
}
