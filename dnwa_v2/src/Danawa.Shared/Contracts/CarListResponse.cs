using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

/// <summary>car_list(IF-CAR-002)와 car_search(IF-CAR-003) 응답이 공유, 필드 구성 동일</summary>
public class CarListResponse : ResponseBase
{
    [JsonPropertyName("cars")]
    public List<CarSummaryDto> Cars { get; set; } = new();
}
