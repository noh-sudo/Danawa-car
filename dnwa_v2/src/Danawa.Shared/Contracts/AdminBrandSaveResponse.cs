using System.Text.Json.Serialization;

namespace Danawa.Shared.Contracts;

public class AdminBrandSaveResponse : ResponseBase
{
    [JsonPropertyName("brand_id")]
    public int BrandId { get; set; }
}
