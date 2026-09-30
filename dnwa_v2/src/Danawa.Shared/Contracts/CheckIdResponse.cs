using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class CheckIdResponse : ResponseBase
{
    [JsonPropertyName("available")]
    public bool Available { get; set; }
}
