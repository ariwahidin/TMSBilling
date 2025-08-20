using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_VENDOR")]
    public class Vendor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        [StringLength(50)]
        public required string SUP_CODE { get; set; }

        [StringLength(10)]
        public string? SUP_TYPE { get; set; }

        [StringLength(50)]
        public string? SUP_NAME { get; set; }

        [StringLength(50)]
        public string? SUP_ADDR1 { get; set; }

        [StringLength(50)]
        public string? SUP_ADDR2 { get; set; }

        [StringLength(50)]
        public string? SUP_CITY { get; set; }

        [StringLength(50)]
        public string? SUP_EMAIL { get; set; }

        [StringLength(20)]
        public string? SUP_TEL { get; set; }

        [StringLength(20)]
        public string? SUP_FAX { get; set; }

        [StringLength(50)]
        public string? SUP_PIC { get; set; }

        [StringLength(25)]
        public string? TAX_REG_NO { get; set; }

        [StringLength(50)]
        public string? ENTRY_USER { get; set; }

        public DateTime? ENTRY_DATE { get; set; }

        [StringLength(10)]
        public string? UPDATE_USER { get; set; }

        public DateTime? UPDATE_DATE { get; set; }

        public byte? ACTIVE_FLAG { get; set; }
    }
}
