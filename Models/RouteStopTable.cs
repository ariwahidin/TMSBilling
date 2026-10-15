using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_ROUTE_STOPS")]
    public class RouteStopTable
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int ROUTE_ID { get; set; }

        [Required]
        public int GEOFENCE_TABLE_ID { get; set; }

        [Required]
        public int SEQUENCE_NO { get; set; }

        [Required]
        [MaxLength(20)]
        public string STOP_TYPE { get; set; } = "DELIVERY";

        public TimeSpan? PLANNED_ARRIVAL { get; set; }

        public int? STOP_DURATION_MINUTES { get; set; }

        [MaxLength(255)]
        public string? NOTES { get; set; }

        [ForeignKey(nameof(ROUTE_ID))]
        public RouteTable? Route { get; set; }

        [ForeignKey(nameof(GEOFENCE_TABLE_ID))]
        public GeofenceTable? Geofence { get; set; }
    }
}