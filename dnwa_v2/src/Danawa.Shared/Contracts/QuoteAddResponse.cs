using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class QuoteAddResponse : ResponseBase
{
    [JsonPropertyName("quote_id")]
    public int QuoteId { get; set; }

    [JsonPropertyName("total_price")]
    public int TotalPrice { get; set; }
}
