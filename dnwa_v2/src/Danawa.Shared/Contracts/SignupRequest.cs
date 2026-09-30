using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class SignupRequest : RequestBase
{
    [JsonPropertyName("login_id")]
    public string LoginId { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}
