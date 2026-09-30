using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class QuoteOptionDto
{
    [JsonPropertyName("option_name")]
    public string OptionName { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public int Price { get; set; }
}
