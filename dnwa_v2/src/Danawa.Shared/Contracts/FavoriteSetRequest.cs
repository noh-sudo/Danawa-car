using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class FavoriteSetRequest : RequestBase
{
    [JsonPropertyName("car_id")]
    public int CarId { get; set; }

    [JsonPropertyName("on")]
    public bool On { get; set; }
}
