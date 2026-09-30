using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class QuoteListResponse : ResponseBase
{
    [JsonPropertyName("quotes")]
    public List<QuoteDto> Quotes { get; set; } = new();
}
