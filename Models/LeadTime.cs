using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_LEADTIME")]
    public class LeadTime
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_seq { get; set; }

        [StringLength(30)]
        public string? cust_code { get; set; }

        [Required]
        [StringLength(100)]
        public string? origin { get; set; }

        [Required]
        [StringLength(100)]
        public string? dest { get; set; }

        [StringLength(10)]
        public string? serv_type { get; set; }

        [StringLength(10)]
        public string? serv_moda { get; set; }

        [StringLength(50)]
        public string? truck_size { get; set; }

        public int? delivery_days { get; set; }
        public int? pod_days { get; set; }

        public int? e_pod_days { get; set; }

        public byte? active_flag { get; set; }

        [StringLength(50)]
        public string? entry_user { get; set; }

        public DateTime? entry_date { get; set; }

        [StringLength(50)]
        public string? update_user { get; set; }

        public DateTime? update_date { get; set; }
    }
}