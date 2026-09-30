using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class FavoriteSetResponse : ResponseBase
{
    [JsonPropertyName("car_id")]
    public int CarId { get; set; }

    [JsonPropertyName("is_favorite")]
    public bool IsFavorite { get; set; }
}
