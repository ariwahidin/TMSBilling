using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TMSBilling.Models
{

    [Table("TRC_PRODUCT")]
    public class ProductTable
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("PRODUCT_ID")]
        [MaxLength(100)]
        public string? ProductID { get; set; }

        [Column("PRODUCT_TYPE_ID")]
        [MaxLength(100)]
        public string? ProductTypeID { get; set; }

        [Column("PRODUCT_TYPE_NAME")]
        [MaxLength(100)]
        public string? ProductTypeName { get; set; }

        [Column("PRODUCT_CATEGORY_ID")]
        [MaxLength(100)]
        public string? ProductCategoryID { get; set; }

        [Column("PRODUCT_CATEGORY_NAME")]
        [MaxLength(100)]
        public string? ProductCategoryName { get; set; }

        [Column("NAME")]
        [MaxLength(100)]
        public string? Name { get; set; }

        [Column("SKU")]
        [MaxLength(50)]
        public string? Sku { get; set; }

        [Column("DESCRIPTION")]
        [MaxLength(255)]
        public string? Description { get; set; }

        [Column("UOM")]
        [MaxLength(20)]
        public string? Uom { get; set; }


        [Column("WIDTH")]
        public decimal? Width { get; set; }

        [Column("LENGTH")]
        public decimal? Length { get; set; }

        [Column("HEIGHT")]
        public decimal? Height { get; set; }

        [Column("CBM")]
        public decimal? Cbm { get; set; }

        [Column("WEIGHT")]
        public decimal? Weight { get; set; }

        [Column("VOLUME")]
        public decimal? Volume { get; set; }

        [Column("PRICE")]
        [DataType(DataType.Currency)]
        public decimal? Price { get; set; }

        [Column("CREATED_AT")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("CREATED_BY")]
        [MaxLength(50)]
        public string? CreatedBy { get; set; }

        [Column("UPDATED_AT")]
        public DateTime? UpdatedAt { get; set; }

        [Column("UPDATED_BY")]
        [MaxLength(50)]
        public string? UpdatedBy { get; set; }
    }

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
