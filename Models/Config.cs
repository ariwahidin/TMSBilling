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

        public required string key { get; set; }


        public required string value { get; set; }

        [Required]
        //[MaxLength(2048)]
        //public required string hostname { get; set; }

        //[StringLength(50)]
        //public int port { get; set; }

        //[StringLength(50)]
        //public int protocol { get; set; }

        [StringLength(50)]
        public string? entryuser { get; set; }

        public DateTime? entrydate { get; set; }

        [StringLength(50)]
        public string? updateuser { get; set; }

        public DateTime? updatedate { get; set; }
    }
}
