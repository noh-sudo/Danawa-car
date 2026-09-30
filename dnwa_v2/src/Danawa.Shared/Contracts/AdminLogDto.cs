using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminLogDto
{
    [JsonPropertyName("login_id")]
    public string LoginId { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("ip_addr")]
    public string IpAddr { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}
