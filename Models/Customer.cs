using Microsoft.AspNetCore.SignalR.Protocol;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_CUSTOMER_MAIN")]
    public class CustomerMain
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [MaxLength(30)]
        public string MAIN_CUST { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? CUST_NAME { get; set; }

        [MaxLength(50)]
        public string? CUST_ADDR1 { get; set; }

        [MaxLength(50)]
        public string? CUST_ADDR2 { get; set; }

        [MaxLength(50)]
        public string? CUST_CITY { get; set; }

        [MaxLength(50)]
        public string? CUST_EMAIL { get; set; }

        [MaxLength(20)]
        public string? CUST_TEL { get; set; }

        [MaxLength(20)]
        public string? CUST_FAX { get; set; }

        [MaxLength(50)]
        public string? CUST_PIC { get; set; }

        [MaxLength(25)]
        public string? TAX_REG_NO { get; set; }

        [MaxLength(25)]
        public string? TAX_IMG { get; set; }

        [MaxLength(50)]
        public string? ENTRY_USER { get; set; }

        public DateTime? ENTRY_DATE { get; set; }

        [MaxLength(10)]
        public string? UPDATE_USER { get; set; }

        public DateTime? UPDATE_DATE { get; set; }

        public int? STATUS_FLAG { get; set; }
    }


    [Table("TRC_CUSTOMER")]
    public class Customer
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(50)]
        public string CUST_CODE { get; set; } = string.Empty;

        [StringLength(50)]
        public string? MCEASY_CUST_ID { get; set; }

        [StringLength(100)]
        public string CUST_NAME { get; set; } = string.Empty;
        
        public string CUST_ADDR1 { get; set; } = string.Empty;
        public string CUST_ADDR2 { get; set; } = string.Empty;
        public string CUST_CITY {  get; set; } = string.Empty;
        public string CUST_EMAIL {  get; set; } = string.Empty;
        public string CUST_TEL {  get; set; } = string.Empty;
        public string CUST_FAX {  get; set; } = string.Empty;
        public string CUST_PIC { get; set; } = string.Empty;
        public string TAX_REG_NO {  get; set; } = string.Empty;
        public string ENTRY_USER {  get; set; } = string.Empty;
        public DateTime? ENTRY_DATE {  get; set; }
        public string UPDATE_USER {  get; set; } = string.Empty;
        public DateTime? UPDATE_DATE { get; set; }
        public int ACTIVE_FLAG { get; set; } = 1;
        public int API_FLAG { get; set; } = 0;
        public string MAIN_CUST {  get; set; } = string.Empty;
        public int CUST_CUTOFF { get; set; }
    }

    [Table("TRC_CUST_GROUP")]
    public class CustomerGroup
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [MaxLength(50)]
        public string SUB_CODE { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CUST_CODE { get; set; } = string.Empty;

        [MaxLength(50)]
        public string MAIN_CUST { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? MCEASY_CUST_ID { get; set; }

        [MaxLength(50)]
        public string? ENTRY_USER { get; set; }

        public DateTime? ENTRY_DATE { get; set; }

        [MaxLength(50)]
        public string? UPDATE_USER { get; set; }

        public DateTime? UPDATE_DATE { get; set; }
    }
}
