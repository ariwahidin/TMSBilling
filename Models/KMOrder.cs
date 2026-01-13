using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_KM")]
    public class KMOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        public int? order_id { get; set; }

        [Required]
        [StringLength(50)]
        public string? inv_no { get; set; }

        public int? km {  get; set; }

        [StringLength(50)]
        public string? entryuser { get; set; }

        public DateTime? entrydate { get; set; }

        [StringLength(50)]
        public string? updateuser { get; set; }

        public DateTime? updatedate { get; set; }
    }

}
