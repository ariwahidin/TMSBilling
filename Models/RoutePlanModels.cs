using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    public static class RoutePlanStatuses
    {
        public const string Draft = "DRAFT";
        public const string Confirmed = "CONFIRMED";
        public const string InProgress = "IN_PROGRESS";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";
        public static readonly string[] All = { Draft, Confirmed, InProgress, Completed, Cancelled };
    }

    public static class RoutePlanStopStatuses
    {
        public const string Pending = "PENDING";
        public const string Arrived = "ARRIVED";
        public const string Completed = "COMPLETED";
        public static readonly string[] All = { Pending, Arrived, Completed };
    }


    [Table("TRC_ROUTE_PLAN_H")]
    public class RoutePlanHeader
    {
        [Key, Column("ID")]
        public int Id { get; set; }

        [Required, StringLength(30), Column("PLAN_NO")]
        public string PlanNo { get; set; } = string.Empty;

        [Column("PLAN_DATE", TypeName = "date")]
        public DateTime PlanDate { get; set; } = DateTime.Today;

        [Column("ROUTE_ID")]
        public int RouteId { get; set; }

        [Required, StringLength(50), Column("ROUTE_CODE")]
        public string RouteCode { get; set; } = string.Empty;

        [Required, StringLength(150), Column("ROUTE_NAME")]
        public string RouteName { get; set; } = string.Empty;

        [StringLength(50), Column("CUSTOMER_CODE")]
        public string? CustomerCode { get; set; }

        [StringLength(50), Column("SUPPLIER_CODE")]
        public string? SupplierCode { get; set; }

        [Column("PLANNED_DEPARTURE")]
        public DateTime? PlannedDeparture { get; set; }

        // Vehicle
        [Column("VEHICLE_ID")]
        public int? VehicleId { get; set; }

        [StringLength(50), Column("VEHICLE_NO")]
        public string? VehicleNo { get; set; }

        // Driver
        [Column("DRIVER_ID")]
        public int? DriverId { get; set; }

        [StringLength(100), Column("EXTERNAL_FLEET_TASK_ID")]
        public string? ExternalFleetTaskId { get; set; }

        [StringLength(100), Column("DRIVER_NAME")]
        public string? DriverName { get; set; }

        // =====================================================
        // TARIFF CONFIGURATION
        // =====================================================

        [StringLength(50), Column("TARIFF_SERV_TYPE")]
        public string? TariffServType { get; set; }

        [StringLength(50), Column("TARIFF_SERV_MODA")]
        public string? TariffServModa { get; set; }

        [StringLength(50), Column("TARIFF_TRUCK_SIZE")]
        public string? TariffTruckSize { get; set; }

        [StringLength(50), Column("TARIFF_CHARGE_UOM")]
        public string? TariffChargeUom { get; set; }

        // =====================================================
        // PRICE BUY SNAPSHOT
        // =====================================================

        [Column("PRICE_BUY_ID")]
        public int? PriceBuyId { get; set; }

        [Column("PRICE_BUY_VALID_DATE")]
        public DateTime? PriceBuyValidDate { get; set; }

        [StringLength(10), Column("PRICE_BUY_CURRENCY")]
        public string? PriceBuyCurrency { get; set; }

        [Column("PRICE_BUY_RATE_VALUE", TypeName = "money")]
        public decimal? PriceBuyRateValue { get; set; }

        [Column("PRICE_BUY_FLAG_MIN")]
        public byte? PriceBuyFlagMin { get; set; }

        [Column("PRICE_BUY_CHARGE_MIN", TypeName = "money")]
        public decimal? PriceBuyChargeMin { get; set; }

        [Column("PRICE_BUY_FLAG_RANGE")]
        public byte? PriceBuyFlagRange { get; set; }

        [Column("PRICE_BUY_MIN_RANGE", TypeName = "decimal(7,2)")]
        public decimal? PriceBuyMinRange { get; set; }

        [Column("PRICE_BUY_MAX_RANGE", TypeName = "decimal(7,2)")]
        public decimal? PriceBuyMaxRange { get; set; }

        [Column("PRICE_BUY_1", TypeName = "money")]
        public decimal? PriceBuy1 { get; set; }

        [Column("PRICE_BUY_2", TypeName = "money")]
        public decimal? PriceBuy2 { get; set; }

        [Column("PRICE_BUY_3", TypeName = "money")]
        public decimal? PriceBuy3 { get; set; }

        [Column("PRICE_BUY_RET_EMPT", TypeName = "money")]
        public decimal? PriceBuyRetEmpt { get; set; }

        [Column("PRICE_BUY_RET_CARGO", TypeName = "money")]
        public decimal? PriceBuyRetCargo { get; set; }

        [Column("PRICE_BUY_OVNIGHT", TypeName = "money")]
        public decimal? PriceBuyOvnnight { get; set; }

        [Column("PRICE_BUY_CANCEL", TypeName = "money")]
        public decimal? PriceBuyCancel { get; set; }

        [Column("PRICE_BUY_TRIP2", TypeName = "money")]
        public decimal? PriceBuyTrip2 { get; set; }

        [Column("PRICE_BUY_TRIP3", TypeName = "money")]
        public decimal? PriceBuyTrip3 { get; set; }

        [Column("PRICE_BUY_DIFF_AREA", TypeName = "money")]
        public decimal? PriceBuyDiffArea { get; set; }

        [Column("PRICE_BUY_RATE_BBM", TypeName = "money")]
        public decimal? PriceBuyRateBbm { get; set; }

        [Column("PRICE_BUY_RATE_TOLL", TypeName = "money")]
        public decimal? PriceBuyRateToll { get; set; }

        // =====================================================
        // PRICE SELL SNAPSHOT
        // =====================================================

        [Column("PRICE_SELL_ID")]
        public int? PriceSellId { get; set; }

        [Column("PRICE_SELL_VALID_DATE")]
        public DateTime? PriceSellValidDate { get; set; }

        [StringLength(10), Column("PRICE_SELL_CURRENCY")]
        public string? PriceSellCurrency { get; set; }

        [Column("PRICE_SELL_RATE_VALUE", TypeName = "money")]
        public decimal? PriceSellRateValue { get; set; }

        [Column("PRICE_SELL_FLAG_MIN")]
        public byte? PriceSellFlagMin { get; set; }

        [Column("PRICE_SELL_CHARGE_MIN", TypeName = "decimal(18,2)")]
        public decimal? PriceSellChargeMin { get; set; }

        [Column("PRICE_SELL_FLAG_RANGE")]
        public byte? PriceSellFlagRange { get; set; }

        [Column("PRICE_SELL_MIN_RANGE", TypeName = "decimal(7,2)")]
        public decimal? PriceSellMinRange { get; set; }

        [Column("PRICE_SELL_MAX_RANGE", TypeName = "decimal(7,2)")]
        public decimal? PriceSellMaxRange { get; set; }

        [Column("PRICE_SELL_1", TypeName = "money")]
        public decimal? PriceSell1 { get; set; }

        [Column("PRICE_SELL_2", TypeName = "money")]
        public decimal? PriceSell2 { get; set; }

        [Column("PRICE_SELL_3", TypeName = "money")]
        public decimal? PriceSell3 { get; set; }

        [Column("PRICE_SELL_RET_EMPTY", TypeName = "money")]
        public decimal? PriceSellRetEmpty { get; set; }

        [Column("PRICE_SELL_RET_CARGO", TypeName = "money")]
        public decimal? PriceSellRetCargo { get; set; }

        [Column("PRICE_SELL_OVNIGHT", TypeName = "money")]
        public decimal? PriceSellOvnnight { get; set; }

        [Column("PRICE_SELL_CANCEL", TypeName = "money")]
        public decimal? PriceSellCancel { get; set; }

        [Column("PRICE_SELL_TRIP2", TypeName = "money")]
        public decimal? PriceSellTrip2 { get; set; }

        [Column("PRICE_SELL_TRIP3", TypeName = "money")]
        public decimal? PriceSellTrip3 { get; set; }

        [Column("PRICE_SELL_DIFF_AREA", TypeName = "money")]
        public decimal? PriceSellDiffArea { get; set; }

        [Column("PRICE_SELL_RATE_TOLL", TypeName = "money")]
        public decimal? PriceSellRateToll { get; set; }

        [Column("PRICE_SELL_RATE_TRUCK", TypeName = "money")]
        public decimal? PriceSellRateTruck { get; set; }

        [Column("PRICE_SELL_RATE_DRIVER", TypeName = "money")]
        public decimal? PriceSellRateDriver { get; set; }

        // =====================================================
        // STATUS & AUDIT
        // =====================================================

        [Required, StringLength(20), Column("STATUS")]
        public string Status { get; set; } = RoutePlanStatuses.Draft;

        [StringLength(1000), Column("NOTES")]
        public string? Notes { get; set; }

        [Required, StringLength(100), Column("CREATED_BY")]
        public string CreatedBy { get; set; } = "System";

        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(100), Column("UPDATED_BY")]
        public string? UpdatedBy { get; set; }

        [Column("UPDATED_AT")]
        public DateTime? UpdatedAt { get; set; }

        [StringLength(100), Column("CONFIRMED_BY")]
        public string? ConfirmedBy { get; set; }

        [Column("CONFIRMED_AT")]
        public DateTime? ConfirmedAt { get; set; }

        [StringLength(100), Column("STARTED_BY")]
        public string? StartedBy { get; set; }

        [Column("STARTED_AT")]
        public DateTime? StartedAt { get; set; }

        [StringLength(100), Column("COMPLETED_BY")]
        public string? CompletedBy { get; set; }

        [Column("COMPLETED_AT")]
        public DateTime? CompletedAt { get; set; }

        // Navigation
        public ICollection<RoutePlanStop> Stops { get; set; }
            = new List<RoutePlanStop>();
    }

    //[Table("TRC_ROUTE_PLAN_H")]
    //public class RoutePlanHeader
    //{
    //    [Key, Column("ID")]
    //    public int Id { get; set; }

    //    [Required, StringLength(30), Column("PLAN_NO")]
    //    public string PlanNo { get; set; } = string.Empty;

    //    [Column("PLAN_DATE", TypeName = "date")]
    //    public DateTime PlanDate { get; set; } = DateTime.Today;

    //    [Column("ROUTE_ID")]
    //    public int RouteId { get; set; }

    //    [Required, StringLength(50), Column("ROUTE_CODE")]
    //    public string RouteCode { get; set; } = string.Empty;

    //    [Required, StringLength(150), Column("ROUTE_NAME")]
    //    public string RouteName { get; set; } = string.Empty;

    //    [StringLength(50), Column("CUSTOMER_CODE")]
    //    public string? CustomerCode { get; set; }

    //    [StringLength(50)]
    //    public string? SupplierCode { get; set; }

    //    [Column("PLANNED_DEPARTURE")]
    //    public DateTime? PlannedDeparture { get; set; }

    //    // External Fleet vehicle ID and human-readable license plate snapshot.
    //    [Column("VEHICLE_ID")]
    //    public int? VehicleId { get; set; }

    //    [StringLength(50), Column("VEHICLE_NO")]
    //    public string? VehicleNo { get; set; }


    //    [Column("DRIVER_ID")]
    //    public int? DriverId { get; set; }

    //    [StringLength(100), Column("EXTERNAL_FLEET_TASK_ID")]
    //    public string? ExternalFleetTaskId { get; set; }

    //    [StringLength(100), Column("DRIVER_NAME")]
    //    public string? DriverName { get; set; }

    //    [Required, StringLength(20), Column("STATUS")]
    //    public string Status { get; set; } = RoutePlanStatuses.Draft;

    //    [StringLength(1000), Column("NOTES")]
    //    public string? Notes { get; set; }

    //    [Required, StringLength(100), Column("CREATED_BY")]
    //    public string CreatedBy { get; set; } = "System";

    //    [Column("CREATED_AT")]
    //    public DateTime CreatedAt { get; set; } = DateTime.Now;

    //    [StringLength(100), Column("UPDATED_BY")]
    //    public string? UpdatedBy { get; set; }

    //    [Column("UPDATED_AT")]
    //    public DateTime? UpdatedAt { get; set; }

    //    [StringLength(100), Column("CONFIRMED_BY")]
    //    public string? ConfirmedBy { get; set; }

    //    [Column("CONFIRMED_AT")]
    //    public DateTime? ConfirmedAt { get; set; }

    //    [StringLength(100), Column("STARTED_BY")]
    //    public string? StartedBy { get; set; }

    //    [Column("STARTED_AT")]
    //    public DateTime? StartedAt { get; set; }

    //    [StringLength(100), Column("COMPLETED_BY")]
    //    public string? CompletedBy { get; set; }

    //    [Column("COMPLETED_AT")]
    //    public DateTime? CompletedAt { get; set; }

    //    public ICollection<RoutePlanStop> Stops { get; set; } = new List<RoutePlanStop>();
    //}

    [Table("TRC_ROUTE_PLAN_STOPS")]
    public class RoutePlanStop
    {
        [Key, Column("ID")]
        public int Id { get; set; }

        [Column("ROUTE_PLAN_ID")]
        public int RoutePlanId { get; set; }

        [Column("MASTER_ROUTE_STOP_ID")]
        public int? MasterRouteStopId { get; set; }

        [Column("GEOFENCE_TABLE_ID")]
        public int GeofenceTableId { get; set; }

        [Column("SEQUENCE_NO")]
        public int SequenceNo { get; set; }

        [Required, StringLength(30), Column("STOP_TYPE")]
        public string StopType { get; set; } = "WAYPOINT";

        [Required, StringLength(150), Column("FENCE_NAME")]
        public string FenceName { get; set; } = string.Empty;

        [StringLength(500), Column("ADDRESS")]
        public string? Address { get; set; }

        [StringLength(100), Column("CITY")]
        public string? City { get; set; }

        [Column("PLANNED_ARRIVAL")]
        public TimeSpan? PlannedArrival { get; set; }

        [Column("PLANNED_DEPARTURE", TypeName = "time")]
        public TimeSpan? PlannedDeparture { get; set; }

        [Column("STOP_DURATION_MINUTES")]
        public int StopDurationMinutes { get; set; }

        [Required, StringLength(20), Column("STATUS")]
        public string Status { get; set; } = RoutePlanStopStatuses.Pending;

        [Column("ACTUAL_ARRIVAL_AT")]
        public DateTime? ActualArrivalAt { get; set; }

        [Column("ACTUAL_DEPARTURE_AT", TypeName = "datetime2")]
        public DateTime? ActualDepartureAt { get; set; }

        [Column("COMPLETED_AT")]
        public DateTime? CompletedAt { get; set; }

        [StringLength(1000), Column("NOTES")]
        public string? Notes { get; set; }

        [ForeignKey(nameof(RoutePlanId))]
        public RoutePlanHeader? RoutePlan { get; set; }

        public ICollection<RoutePlanStopHistory> History { get; set; } = new List<RoutePlanStopHistory>();
    }

    [Table("TRC_ROUTE_PLAN_STOP_HISTORY")]
    public class RoutePlanStopHistory
    {
        [Key, Column("ID")]
        public long Id { get; set; }

        [Column("ROUTE_PLAN_ID")]
        public int RoutePlanId { get; set; }

        [Column("ROUTE_PLAN_STOP_ID")]
        public int RoutePlanStopId { get; set; }

        [StringLength(20), Column("PREVIOUS_STATUS")]
        public string? PreviousStatus { get; set; }

        [Required, StringLength(20), Column("NEW_STATUS")]
        public string NewStatus { get; set; } = RoutePlanStopStatuses.Pending;

        [Required, StringLength(20), Column("ACTION_TYPE")]
        public string ActionType { get; set; } = "STATUS_CHANGE";

        [StringLength(1000), Column("REASON")]
        public string? Reason { get; set; }

        [Required, StringLength(100), Column("CHANGED_BY")]

        public string ChangedBy { get; set; } = "System";

        [Column("CHANGED_AT")]
        public DateTime ChangedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(RoutePlanStopId))]
        public RoutePlanStop? Stop { get; set; }
    }
}
