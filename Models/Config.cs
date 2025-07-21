using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_CONFIG")]
    public class Config
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        [StringLength(20)]
        public required string hostname { get; set; }

        [StringLength(50)]
        public int port { get; set; }

        [StringLength(50)]
        public string? entryuser { get; set; }

        public DateTime? entrydate { get; set; }

        [StringLength(50)]
        public string? updateuser { get; set; }

        public DateTime? updatedate { get; set; }
    }
}
