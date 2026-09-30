using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminLogListResponse : ResponseBase
{
    [JsonPropertyName("logs")]
    public List<AdminLogDto> Logs { get; set; } = new();
}
