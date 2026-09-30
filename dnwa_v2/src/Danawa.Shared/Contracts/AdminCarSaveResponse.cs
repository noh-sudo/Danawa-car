using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminCarSaveResponse : ResponseBase
{
    [JsonPropertyName("car_id")]
    public int CarId { get; set; }
}
