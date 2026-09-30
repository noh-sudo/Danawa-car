using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminOptionDto
{
    [JsonPropertyName("option_id")]
    public int OptionId { get; set; }

    [JsonPropertyName("option_name")]
    public string OptionName { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public int Price { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
}
