using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class MainDailyTopResponse : ResponseBase
{
    [JsonPropertyName("car")]
    public DailyTopCarDto? Car { get; set; }
}
