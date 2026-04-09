using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("MST_HOLIDAY")]
    public class MstHoliday
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id_seq")]
        public int IdSeq { get; set; }

        [Required]
        [Column("hol_date")]
        public DateOnly HolDate { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("hol_name")]
        public string HolName { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }
    }
}
