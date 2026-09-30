using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class CarDetailRequest : RequestBase
{
    [JsonPropertyName("car_id")]
    public int CarId { get; set; }
}
