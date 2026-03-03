using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_VENDOR_TRUCK_EMAIL")]
    public class VendorTruckEmail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        [StringLength(50)]
        public required string sup_code { get; set; }

        //[Required]
        [StringLength(20)]
        public string? vehicle_no { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public required string email_address { get; set; }

        [Required]
        [StringLength(10)]
        public required string email_type { get; set; } // "TO" or "CC"

        [StringLength(100)]
        public string? email_name { get; set; }

        public byte? is_active { get; set; }

        [StringLength(100)]
        public string? remark { get; set; }

        public DateTime? entry_date { get; set; }

        [StringLength(50)]
        public string? entry_user { get; set; }

        public DateTime? update_date { get; set; }

        [StringLength(50)]
        public string? update_user { get; set; }

        // Navigation / display (not mapped)
        [NotMapped]
        public string? sup_name { get; set; }
    }
}