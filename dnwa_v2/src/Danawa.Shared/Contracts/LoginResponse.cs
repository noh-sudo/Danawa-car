using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class LoginResponse : ResponseBase
{
    [JsonPropertyName("login_id")]
    public string LoginId { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}
