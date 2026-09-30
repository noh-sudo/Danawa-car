using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminOptionListRequest : RequestBase
{
    [JsonPropertyName("car_id")]
    public int CarId { get; set; }
}
