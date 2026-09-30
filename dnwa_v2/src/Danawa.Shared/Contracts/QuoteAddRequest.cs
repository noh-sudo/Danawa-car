using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class QuoteAddRequest : RequestBase
{
    [JsonPropertyName("car_id")]
    public int CarId { get; set; }

    [JsonPropertyName("option_ids")]
    public List<int> OptionIds { get; set; } = new();
}
