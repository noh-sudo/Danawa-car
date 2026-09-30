using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminCarListResponse : ResponseBase
{
    [JsonPropertyName("cars")]
    public List<AdminCarDto> Cars { get; set; } = new();
}
