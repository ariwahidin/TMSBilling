using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_ROUTES")]
    public class RouteTable
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [MaxLength(30)]
        public string ROUTE_CODE { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string ROUTE_NAME { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? DESCRIPTION { get; set; }

        [Required]
        [MaxLength(30)]
        public string ROUTE_TYPE { get; set; } = "MULTI_STOP";

        [Required]
        [MaxLength(20)]
        public string STATUS { get; set; } = "ACTIVE";

        public DateTime CREATED_AT { get; set; }

        public DateTime? UPDATED_AT { get; set; }

        [MaxLength(50)]
        public string? CREATED_BY { get; set; }

        [MaxLength(50)]
        public string? UPDATED_BY { get; set; }

        public ICollection<RouteStopTable> Stops { get; set; }
            = new List<RouteStopTable>();

        public ICollection<RouteCustomerAssignment> CustomerAssignments { get; set; }
            = new List<RouteCustomerAssignment>();
    }
}