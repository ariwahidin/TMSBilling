using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_ROUTE_CUSTOMERS")]
    public class RouteCustomerAssignment
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int ROUTE_ID { get; set; }

        [Required]
        [MaxLength(50)]
        public string CUSTOMER_CODE { get; set; } = string.Empty;

        public DateTime CREATED_AT { get; set; }

        [ForeignKey(nameof(ROUTE_ID))]
        public RouteTable? Route { get; set; }
    }
}