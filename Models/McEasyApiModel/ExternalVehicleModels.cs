using System.Text.Json.Serialization;

namespace TMSBilling.Models
{
    public sealed class NearestVehicleResponse
    {
        [JsonPropertyName("data")]
        public NearestVehicleData? Data { get; set; }
    }

    public sealed class NearestVehicleData
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("result")]
        public List<ExternalVehicle>? Result { get; set; }
    }

    public sealed class ExternalVehicle
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("license_plate")]
        public string? LicensePlate { get; set; }

        [JsonPropertyName("hull_no")]
        public string? HullNo { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("driver")]
        public ExternalVehicleDriver? Driver { get; set; }
    }

    public sealed class ExternalVehicleDriver
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("is_available")]
        public bool IsAvailable { get; set; }
    }
}
