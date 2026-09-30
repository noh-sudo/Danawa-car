using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminOptionListResponse : ResponseBase
{
    [JsonPropertyName("options")]
    public List<AdminOptionDto> Options { get; set; } = new();
}
