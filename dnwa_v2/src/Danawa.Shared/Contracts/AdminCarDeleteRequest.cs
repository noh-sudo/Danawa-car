using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminCarDeleteRequest : RequestBase
{
    [JsonPropertyName("car_id")]
    public int CarId { get; set; }
}
