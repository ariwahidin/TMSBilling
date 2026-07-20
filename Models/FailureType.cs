using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_FAILURE_TYPE")]
    public class FailureType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        [Required]
        [StringLength(50)]
        public string? failure_type_code { get; set; }

        [StringLength(200)]
        public string? failure_type_description { get; set; }

        [StringLength(50)]
        public string? entryuser { get; set; }

        public DateTime? entrydate { get; set; }

        [StringLength(50)]
        public string? updateuser { get; set; }

        public DateTime? updatedate { get; set; }
    }
}
