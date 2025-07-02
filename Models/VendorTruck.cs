using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_VENDOR_TRUCK")]
    public class VendorTruck
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        [StringLength(50)]
        public required string sup_code { get; set; }

        [Required]
        [StringLength(20)]
        public required string vehicle_no { get; set; }

        [StringLength(50)]
        public string? vehicle_merk { get; set; }

        [StringLength(50)]
        public string? vehicle_type { get; set; }

        [StringLength(50)]
        public string? vehicle_doortype { get; set; }

        [StringLength(50)]
        public string? vehicle_size { get; set; }

        [StringLength(50)]
        public string? vehicle_driver { get; set; }

        [StringLength(50)]
        public string? vehicle_STNK { get; set; }

        public DateTime? vehicle_STNK_exp { get; set; }

        [StringLength(50)]
        public string? vehicle_KIR { get; set; }

        public DateTime? vehicle_KIR_exp { get; set; }

        public DateTime? vehicle_emisi { get; set; }

        public byte? vehicle_active { get; set; }

        [StringLength(70)]
        public string? vehicle_remark { get; set; }

        public DateTime? entry_date { get; set; }

        [StringLength(50)]
        public string? entry_user { get; set; }

        public DateTime? update_date { get; set; }

        [StringLength(50)]
        public string? update_user { get; set; }

        [StringLength(30)]
        public string? vehicle_KTP { get; set; }

        public DateTime? vehicle_SIM { get; set; }
    }
}
