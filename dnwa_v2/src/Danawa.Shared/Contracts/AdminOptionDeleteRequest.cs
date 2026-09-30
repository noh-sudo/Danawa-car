using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminOptionDeleteRequest : RequestBase
{
    [JsonPropertyName("option_id")]
    public int OptionId { get; set; }
}
