using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminCarListRequest : RequestBase
{
    [JsonPropertyName("brand_id")]
    public int? BrandId { get; set; }
}
