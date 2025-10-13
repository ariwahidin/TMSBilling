using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_GEOFENCE")]
    public class GeofenceTable
    {

        [Key]
        [Column("ID")]
        public int? Id { get; set; }
        [Column("GEOFENCE_ID")]
        public int? GeofenceId { get; set; }

        [Column("COMPANY_ID")]
        public int? CompanyId { get; set; }

        [Column("CUSTOMER_ID")]
        [MaxLength(50)]
        public string? CustomerId { get; set; }

        [Column("FENCE_NAME")]
        [MaxLength(100)]
        public string? FenceName { get; set; }

        [Column("TYPE")]
        [MaxLength(50)]
        public string? Type { get; set; }

        [Column("POLY_DATA")]
        public string? PolyData { get; set; }

        [Column("CIRC_DATA")]
        public string? CircData { get; set; }

        [Column("LAT")]
        public string? Lat { get; set; }

        [Column("LONG")]
        public string? Long { get; set; }

        [Column("RADIUS")]
        public string? Radius { get; set; }

        [Column("CORDINATES")]
        public string? Cordinates { get; set; }

        [Column("ADDRESS")]
        [MaxLength(255)]
        public string? Address { get; set; }

        [Column("ADDRESS_DETAIL")]
        [MaxLength(255)]
        public string? AddressDetail { get; set; }

        [Column("PROVINCE")]
        [MaxLength(100)]
        public string? Province { get; set; }

        [Column("CITY")]
        [MaxLength(100)]
        public string? City { get; set; }

        [Column("POSTAL_CODE")]
        [MaxLength(10)]
        public string? PostalCode { get; set; }

        [Column("CATEGORY")]
        [MaxLength(50)]
        public string? Category { get; set; }

        [Column("CONTACT_NAME")]
        [MaxLength(100)]
        public string? ContactName { get; set; }

        [Column("PHONE_NO")]
        [MaxLength(20)]
        public string? PhoneNo { get; set; }

        [Column("IS_GARAGE")]
        public bool IsGarage { get; set; }

        [Column("IS_SERVICE_LOC")]
        public bool IsServiceLoc { get; set; }

        [Column("IS_BILLING_ADDR")]
        public bool IsBillingAddr { get; set; }

        [Column("IS_DEPOT")]
        public bool IsDepot { get; set; }

        [Column("IS_ALERT")]
        public bool IsAlert { get; set; }

        [Column("SERVICE_START")]
        [MaxLength(50)]
        public string? ServiceStart { get; set; }

        [Column("SERVICE_END")]
        [MaxLength(50)]
        public string? ServiceEnd { get; set; }

        [Column("BREAK_START")]
        [MaxLength(50)]
        public string? BreakStart { get; set; }

        [Column("BREAK_END")]
        [MaxLength(50)]
        public string? BreakEnd { get; set; }

        [Column("SERVICE_LOC_TYPE")]
        [MaxLength(50)]
        public string? ServiceLocType { get; set; }

        [Column("CUSTOMER_NAME")]
        [MaxLength(255)]
        public string? CustomerName { get; set; }

        [Column("HAS_RELATION")]
        public bool? HasRelation { get; set; }

        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UPDATED_AT")]
        public DateTime? UpdatedAt { get; set; }
    }
}




