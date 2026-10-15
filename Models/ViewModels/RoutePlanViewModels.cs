using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TMSBilling.Models.ViewModels
{
    public class RoutePlanFormViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(30)]
        public string? PlanNo { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime PlanDate { get; set; } = DateTime.Today;

        [Required]
        public int? RouteId { get; set; }

        [StringLength(50)]
        public string? CustomerCode { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? PlannedDeparture { get; set; }

        [StringLength(50)]
        public string? VehicleNo { get; set; }

        [StringLength(100)]
        public string? DriverName { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public string? Status { get; set; }
        public List<SelectListItem> Routes { get; set; } = new();
        public List<SelectListItem> Customers { get; set; } = new();
        public List<RoutePlanStopPreviewViewModel> Stops { get; set; } = new();
    }

    public class RoutePlanStopPreviewViewModel
    {
        public int SequenceNo { get; set; }
        public string StopType { get; set; } = string.Empty;
        public string FenceName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public TimeSpan? PlannedArrival { get; set; }
        public int StopDurationMinutes { get; set; }
        public string Status { get; set; } = "PENDING";
        public DateTime? ActualArrivalAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int StopId { get; set; }
    }

    public class RoutePlanIndexViewModel
    {
        public string? PlanNo { get; set; }
        public string? RouteCode { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<RoutePlanHeader> Items { get; set; } = new();
    }

    public class RoutePlanStopStatusInput
    {
        [Required]
        public int StopId { get; set; }

        [Required]
        public string NewStatus { get; set; } = string.Empty;
    }

    public class RoutePlanStopCorrectionInput
    {
        [Required]
        public int StopId { get; set; }

        [Required]
        public string NewStatus { get; set; } = string.Empty;

        [Required, StringLength(1000, MinimumLength = 5)]
        public string Reason { get; set; } = string.Empty;
    }
}
