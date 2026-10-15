using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TMSBilling.Models
{
    public class RouteViewModel
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "Route Code is required.")]
        [StringLength(30)]
        public string ROUTE_CODE { get; set; } = string.Empty;

        [Required(ErrorMessage = "Route Name is required.")]
        [StringLength(150)]
        public string ROUTE_NAME { get; set; } = string.Empty;

        [StringLength(255)]
        public string? DESCRIPTION { get; set; }

        [Required]
        public string ROUTE_TYPE { get; set; } = "MULTI_STOP";

        [Required]
        public string STATUS { get; set; } = "ACTIVE";

        public List<string> CUSTOMER_CODES { get; set; } = new();

        public List<RouteStopInput> Stops { get; set; } = new();

        public List<SelectListItem> AvailableLocations { get; set; } = new();

        public List<SelectListItem> AvailableCustomers { get; set; } = new();
    }

    public class RouteStopInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid location.")]
        public int GEOFENCE_TABLE_ID { get; set; }

        [Range(1, 999)]
        public int SEQUENCE_NO { get; set; }

        [Required(ErrorMessage = "Stop Type is required.")]
        public string STOP_TYPE { get; set; } = "DELIVERY";

        public TimeSpan? PLANNED_ARRIVAL { get; set; }

        [Range(0, 1440, ErrorMessage = "Duration must be between 0 and 1440 minutes.")]
        public int? STOP_DURATION_MINUTES { get; set; }

        [StringLength(255)]
        public string? NOTES { get; set; }
    }
}