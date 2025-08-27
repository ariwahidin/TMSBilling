using System.Text.Json.Serialization;

namespace TMSBilling.Models.McEasyApiModel
{
    public class ProductStore
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("product_type_id")]
        public Guid ProductTypeId { get; set; }

        [JsonPropertyName("sku")]
        public string Sku { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("uom")]
        public string Uom { get; set; }

        [JsonPropertyName("weight")]
        public decimal Weight { get; set; }

        [JsonPropertyName("volume")]
        public decimal Volume { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }
}
