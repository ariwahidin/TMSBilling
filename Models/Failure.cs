using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_FAILURE")]
    public class Failure
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        [Required]
        [StringLength(50)]
        public string? failure_code { get; set; }

        [StringLength(100)]
        public string? failure_name { get; set; }

        [StringLength(200)]
        public string? failure_description { get; set; }

        public int? failure_type_id { get; set; }

        [StringLength(50)]
        public string? entryuser { get; set; }

        public DateTime? entrydate { get; set; }

        [StringLength(50)]
        public string? updateuser { get; set; }

        public DateTime? updatedate { get; set; }

        // Navigation property
        [ForeignKey("failure_type_id")]
        public FailureType? FailureType { get; set; }
    }
}
