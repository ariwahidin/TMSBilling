using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_CONSIGNEE")]
    public class Consignee
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Key]
        [Required]
        [StringLength(30)]
        public required string CNEE_CODE { get; set; }

        [StringLength(50)]
        public string? CNEE_NAME { get; set; }

        [StringLength(120)]
        public string? CNEE_ADDR1 { get; set; }

        [StringLength(50)]
        public string? CNEE_ADDR2 { get; set; }

        [StringLength(50)]
        public string? CNEE_ADDR3 { get; set; }

        [StringLength(50)]
        public string? CNEE_ADDR4 { get; set; }

        [StringLength(20)]
        public string? CNEE_TEL { get; set; }

        [StringLength(20)]
        public string? CNEE_FAX { get; set; }

        [StringLength(50)]
        public string? CNEE_PIC { get; set; }

        [StringLength(25)]
        public string? TAX_REG_NO { get; set; }

        public byte? ACTIVE_FLAG { get; set; }

        [Required]
        [StringLength(30)]
        public string? SUB_CODE { get; set; }

        [StringLength(50)]
        public string? ENTRY_USER { get; set; }

        public DateTime? ENTRY_DATE { get; set; }

        [StringLength(10)]
        public string? UPDATE_USER { get; set; }

        public DateTime? UPDATE_DATE { get; set; }
    }
}
