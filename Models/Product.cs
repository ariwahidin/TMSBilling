using System.Text.Json.Serialization;

namespace TMSBilling.Models
{
    public class ProductResponse
    {
        public Metadata? metadata { get; set; }
        public ProductData? data { get; set; }
    }

    public class Metadata
    {
        public int count { get; set; }
        public int page { get; set; }
        public int total_count { get; set; }
        public int total_page { get; set; }
    }

    public class ProductData
    {
        public List<Product>? paginated_result { get; set; }
        public List<string>? ids { get; set; }
    }

    public class OrderLoad {
        public string? id { get; set; }
        public string? name { get; set; }
        public Product? product { get; set; }
        public string? sku { get; set; }
        public string? description { get; set; }
        public string? uom { get; set; }
        public decimal? weight { get; set; }
        public decimal? volume { get; set; }
        public decimal? price { get; set; }
        public decimal? quantity { get; set; }
    }


    public class ProductStore
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("product_type_id")]
        public Guid ProductTypeId { get; set; }

        [JsonPropertyName("sku")]
        public string? Sku { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("uom")]
        public string? Uom { get; set; }

        [JsonPropertyName("weight")]
        public decimal? Weight { get; set; }

        [JsonPropertyName("volume")]
        public decimal Volume { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }

    public class Product
    {
        public string? id { get; set; }
        public string? name { get; set; }

        public ProductType? product_type { get; set; }
        public string? sku { get; set; }
        public string? description { get; set; }
        public string? uom { get; set; }
        public decimal? weight { get; set; }
        public decimal? volume { get; set; }
        public decimal? price { get; set; }
    }

    public class ProductType
    {
        public Guid? id { get; set; }
        public string? name { get; set; }
        public ProductCategory? product_category { get; set; }
    }

    public class ProductTypeStore
    {
        public Guid? id { get; set; }
        public string? name { get; set; }
        public string? product_category_id { get; set; }
    }


    // Product Category
    public class ProductCategory
    {
        public string? id { get; set; }
        public string? name { get; set; }

        public int? min_temperature { get; set; }

        public int? max_temperature { get; set; }
    }

    public class ProductCategoryStore
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("min_temperature")]
        public int? MinTemperature { get; set; }
        [JsonPropertyName("max_temperature")]
        public int? MaxTemperature { get; set; }
    }
}
