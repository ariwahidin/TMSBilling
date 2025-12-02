using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_AREA_GROUP")]
    public class AreaGroup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_seq { get; set; }

        [Required]
        [StringLength(50)]
        public required string area_name { get; set; }

        [StringLength(50)]
        public string? entry_user { get; set; }

        public DateTime? entry_date { get; set; }
    }


    [Table("TRC_ROUTE_TYPE")]
    public class RouteType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_seq { get; set; }

        [Required]
        [StringLength(50)]
        public required string route_name { get; set; }

        [StringLength(50)]
        public string? entry_user { get; set; }

        public DateTime? entry_date { get; set; }
    }



    [Table("TRC_ROUTE_GROUP")]
    public class RouteGroup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_seq { get; set; }

        [Required]
        [StringLength(50)]
        public required string origin { get; set; }

        [Required]
        [StringLength(50)]
        public required string dest { get; set; }

        [Required]
        [StringLength(50)]
        public required string route { get; set; }

        [StringLength(50)]
        public string? entry_user { get; set; }

        public DateTime? entry_date { get; set; }
    }
}
