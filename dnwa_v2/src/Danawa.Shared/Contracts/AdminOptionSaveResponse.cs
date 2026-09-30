using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminOptionSaveResponse : ResponseBase
{
    [JsonPropertyName("option_id")]
    public int OptionId { get; set; }
}
