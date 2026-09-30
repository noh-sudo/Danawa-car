using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class DailyTopCarDto : CarSummaryDto
{
    [JsonPropertyName("quote_count")]
    public int QuoteCount { get; set; }
}
