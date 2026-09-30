using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class FavoriteListResponse : ResponseBase
{
    [JsonPropertyName("favorites")]
    public List<FavoriteDto> Favorites { get; set; } = new();
}
