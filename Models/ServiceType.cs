using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_SERV_TYPE")]
    public class ServiceType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_seq { get; set; }

        [Required]
        [StringLength(50)]
        public required string serv_name { get; set; }

        [StringLength(50)]
        public string? entry_user { get; set; }

        public DateTime? entry_date { get; set; }
    }
}
