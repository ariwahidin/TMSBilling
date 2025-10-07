using System.Text.Json.Serialization;
namespace TMSBilling.Models.McEasyApiModel
{
    public class PayloadCreateOrder
    {
        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("billed_customer_id")]
        public string? BilledCustomerId { get; set; }

        [JsonPropertyName("destination_address_id")]
        public int DestinationAddressId { get; set; }

        [JsonPropertyName("expected_delivered_on")]
        public string? ExpectedDeliveredOn { get; set; }   // 👉 string, bukan DateTime

        [JsonPropertyName("expected_pickup_on")]
        public string? ExpectedPickupOn { get; set; }      // 👉 string juga

        [JsonPropertyName("origin_address_id")]
        public int OriginAddressId { get; set; }

        [JsonPropertyName("shipment_number")]
        public string? ShipmentNumber { get; set; }
    }
}
