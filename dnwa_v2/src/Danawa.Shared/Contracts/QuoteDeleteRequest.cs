using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class QuoteDeleteRequest : RequestBase
{
    [JsonPropertyName("quote_id")]
    public int QuoteId { get; set; }
}
