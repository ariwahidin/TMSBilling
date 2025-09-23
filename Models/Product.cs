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
        public string? id { get; set; }
        public string? name { get; set; }
        public ProductCategory? product_category { get; set; }
    }

    public class ProductCategory
    {
        public string? id { get; set; }
        public string? name { get; set; }
    }
}
