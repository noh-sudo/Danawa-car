using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class CheckIdRequest : RequestBase
{
    [JsonPropertyName("login_id")]
    public string LoginId { get; set; } = string.Empty;
}
