using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminStatsWeeklyTop5Response : ResponseBase
{
    [JsonPropertyName("domestic")]
    public List<WeeklyTopCarDto> Domestic { get; set; } = new();

    [JsonPropertyName("imported")]
    public List<WeeklyTopCarDto> Imported { get; set; } = new();
}
