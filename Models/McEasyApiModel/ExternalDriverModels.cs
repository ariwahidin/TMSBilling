using System.Text.Json.Serialization;

namespace TMSBilling.Models
{
    public sealed class ExternalDriverListResponse
    {
        [JsonPropertyName("metadata")]
        public ExternalDriverMetadata? Metadata { get; set; }

        [JsonPropertyName("data")]
        public ExternalDriverResponseData? Data { get; set; }
    }

    public sealed class ExternalDriverMetadata
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("total_page")]
        public int TotalPage { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }
    }

    public sealed class ExternalDriverResponseData
    {
        [JsonPropertyName("paginated_result")]
        public List<ExternalDriver>? PaginatedResult { get; set; }

        [JsonPropertyName("ids")]
        public List<int>? Ids { get; set; }
    }

    public sealed class ExternalDriver
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("position")]
        public ExternalDriverPosition? Position { get; set; }

        [JsonPropertyName("phone_no")]
        public string? PhoneNo { get; set; }
    }

    public sealed class ExternalDriverPosition
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
