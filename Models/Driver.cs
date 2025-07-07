using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_DRIVER")]
    public class Driver
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        //[Required]
        //[StringLength(30)]
        //public required string driver_id { get; set; }

        [StringLength(30)]
        public string? driver_id { get; set; }

        [Required]
        [StringLength(50)]
        public required string driver_name { get; set; }

        [Required]
        [StringLength(50)]
        public required string vendor_id { get; set; }

        public DateTime? driver_birth { get; set; }

        [StringLength(300)]
        public string? driver_address { get; set; }

        [StringLength(50)]
        public string? driver_sim { get; set; }

        public DateTime? driver_sim_exp { get; set; }

        [StringLength(50)]
        public string? driver_nik { get; set; }

        public byte? driver_status { get; set; }

        [StringLength(50)]
        public string? driver_remark { get; set; }

        public DateTime? driver_date_terminate { get; set; }

        [StringLength(50)]
        public string? terminate_reason { get; set; }

        [StringLength(50)]
        public string? user_entry { get; set; }

        public DateTime? date_entry { get; set; }

        [StringLength(50)]
        public string? user_update { get; set; }

        public DateTime? date_update { get; set; }

        [StringLength(50)]
        public string? vehicle_type { get; set; }

        [StringLength(200)]
        public string? driver_photo { get; set; }
    }
}
