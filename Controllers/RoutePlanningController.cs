
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Models.ViewModels;

namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class RoutePlanningController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RoutePlanningController> _logger;

        public RoutePlanningController(
            AppDbContext context,
            ILogger<RoutePlanningController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =========================================================
        // INDEX
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(RoutePlanIndexViewModel filter)
        {
            var query = _context.RoutePlans
                .AsNoTracking()
                .Include(x => x.Stops)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.PlanNo))
            {
                var planNo = filter.PlanNo.Trim();
                query = query.Where(x => x.PlanNo.Contains(planNo));
            }

            if (!string.IsNullOrWhiteSpace(filter.RouteCode))
            {
                var routeCode = filter.RouteCode.Trim();
                query = query.Where(x => x.RouteCode.Contains(routeCode));
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(x => x.Status == filter.Status);
            }

            if (filter.StartDate.HasValue)
            {
                var startDate = filter.StartDate.Value.Date;
                query = query.Where(x => x.PlanDate >= startDate);
            }

            if (filter.EndDate.HasValue)
            {
                var endDate = filter.EndDate.Value.Date.AddDays(1);
                query = query.Where(x => x.PlanDate < endDate);
            }

            filter.Items = await query
                .OrderByDescending(x => x.PlanDate)
                .ThenByDescending(x => x.Id)
                .Take(500)
                .ToListAsync();

            return View(filter);
        }

        // =========================================================
        // CREATE / EDIT FORM
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Form(int? id)
        {
            var vm = new RoutePlanFormViewModel
            {
                PlanDate = DateTime.Today,
                Status = RoutePlanStatuses.Draft
            };

            if (id.HasValue)
            {
                var plan = await _context.RoutePlans
                    .AsNoTracking()
                    .Include(x => x.Stops)
                    .FirstOrDefaultAsync(x => x.Id == id.Value);

                if (plan == null)
                {
                    return NotFound();
                }

                if (plan.Status != RoutePlanStatuses.Draft)
                {
                    TempData["Error"] =
                        "Only DRAFT route plans can be edited.";

                    return RedirectToAction(
                        nameof(Details),
                        new { id = plan.Id });
                }

                vm.Id = plan.Id;
                vm.PlanNo = plan.PlanNo;
                vm.PlanDate = plan.PlanDate;
                vm.RouteId = plan.RouteId;
                vm.CustomerCode = plan.CustomerCode;
                vm.PlannedDeparture = plan.PlannedDeparture;
                vm.VehicleNo = plan.VehicleNo;
                vm.DriverName = plan.DriverName;
                vm.Notes = plan.Notes;
                vm.Status = plan.Status;

                vm.Stops = plan.Stops
                    .OrderBy(s => s.SequenceNo)
                    .Select(s => new RoutePlanStopPreviewViewModel
                    {
                        StopId = s.Id,
                        SequenceNo = s.SequenceNo,
                        StopType = s.StopType,
                        FenceName = s.FenceName,
                        Address = s.Address,
                        City = s.City,
                        PlannedArrival = s.PlannedArrival,
                        StopDurationMinutes = s.StopDurationMinutes,
                        Status = s.Status,
                        ActualArrivalAt = s.ActualArrivalAt,
                        CompletedAt = s.CompletedAt
                    })
                    .ToList();
            }

            await PopulateRoutes(vm);

            if (vm.RouteId.HasValue)
            {
                await PopulateCustomers(vm, vm.RouteId.Value);
            }
            else
            {
                vm.Customers = new List<SelectListItem>();
            }

            return View(vm);
        }

        // =========================================================
        // SAVE ROUTE PLAN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(RoutePlanFormViewModel vm)
        {
            var username =
                HttpContext.Session.GetString("username") ?? "System";

            // PlanNo is generated automatically by the server.
            if (vm.Id == 0)
            {
                ModelState.Remove(nameof(vm.PlanNo));
            }

            // Status is controlled by the server.
            ModelState.Remove(nameof(vm.Status));

            // -----------------------------------------------------
            // 1. Basic validation
            // -----------------------------------------------------
            if (!vm.RouteId.HasValue || vm.RouteId.Value <= 0)
            {
                ModelState.AddModelError(
                    nameof(vm.RouteId),
                    "Please select a Master Route.");
            }

            if (vm.PlanDate == default)
            {
                ModelState.AddModelError(
                    nameof(vm.PlanDate),
                    "Plan Date is required.");
            }

            //if (!ModelState.IsValid)
            //{
            //    await RepopulateFormAsync(vm);
            //    return View("Form", vm);
            //}

            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e =>
                        $"{x.Key}: {e.ErrorMessage}"))
                    .ToList();

                _logger.LogWarning(
                    "Route Plan validation failed: {Errors}",
                    string.Join(" | ", validationErrors));

                await RepopulateFormAsync(vm);
                return View("Form", vm);
            }

            // -----------------------------------------------------
            // 2. Load ACTIVE Master Route
            //
            // IMPORTANT:
            // Master Route properties use uppercase names:
            // ID, ROUTE_CODE, ROUTE_NAME, STATUS.
            // -----------------------------------------------------
            var route = await _context.Routes
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.ID == vm.RouteId!.Value &&
                    r.STATUS == "ACTIVE");

            if (route == null)
            {
                ModelState.AddModelError(
                    nameof(vm.RouteId),
                    "The selected route does not exist or is inactive.");

                await RepopulateFormAsync(vm);
                return View("Form", vm);
            }

            // -----------------------------------------------------
            // 3. Load stops from the current Master Route
            //
            // Master Route stop properties:
            // ROUTE_ID, GEOFENCE_TABLE_ID, SEQUENCE_NO,
            // STOP_TYPE, PLANNED_ARRIVAL,
            // STOP_DURATION_MINUTES, NOTES.
            // -----------------------------------------------------
            var masterStops = await (
                from rs in _context.RouteStops
                join g in _context.Geofences
                    on rs.GEOFENCE_TABLE_ID equals g.Id
                where rs.ROUTE_ID == route.ID
                orderby rs.SEQUENCE_NO
                select new
                {
                    rs.ID,
                    rs.GEOFENCE_TABLE_ID,
                    rs.SEQUENCE_NO,
                    rs.STOP_TYPE,
                    rs.PLANNED_ARRIVAL,
                    rs.STOP_DURATION_MINUTES,
                    rs.NOTES,
                    g.FenceName,
                    g.Address,
                    g.City
                })
                .ToListAsync();

            if (masterStops.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(vm.RouteId),
                    "The selected Master Route has no valid stops.");

                await RepopulateFormAsync(vm);
                return View("Form", vm);
            }

            // -----------------------------------------------------
            // 4. Validate customer assignment
            //
            // RouteCustomerAssignment.CUSTOMER_CODE stores
            // CustomerGroup.CUST_CODE.
            // -----------------------------------------------------
            var assignedCustomerCodes = await _context
                .RouteCustomerAssignments
                .AsNoTracking()
                .Where(x => x.ROUTE_ID == route.ID)
                .Select(x => x.CUSTOMER_CODE)
                .Distinct()
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(vm.CustomerCode))
            {
                vm.CustomerCode = vm.CustomerCode.Trim();

                var customerAssigned = assignedCustomerCodes.Any(
                    code => string.Equals(
                        code,
                        vm.CustomerCode,
                        StringComparison.OrdinalIgnoreCase));

                if (!customerAssigned)
                {
                    ModelState.AddModelError(
                        nameof(vm.CustomerCode),
                        "The selected customer is not assigned to this Master Route.");

                    await RepopulateFormAsync(vm);
                    return View("Form", vm);
                }
            }

            // -----------------------------------------------------
            // 5. Begin transaction
            // -----------------------------------------------------
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                RoutePlanHeader plan;

                if (vm.Id == 0)
                {
                    // CREATE
                    plan = new RoutePlanHeader
                    {
                        PlanNo = await GeneratePlanNoAsync(),
                        CreatedBy = username,
                        CreatedAt = DateTime.Now,
                        Status = RoutePlanStatuses.Draft
                    };

                    _context.RoutePlans.Add(plan);
                }
                else
                {
                    // EDIT
                    plan = await _context.RoutePlans
                        .Include(x => x.Stops)
                        .FirstOrDefaultAsync(x => x.Id == vm.Id);

                    if (plan == null)
                    {
                        await transaction.RollbackAsync();
                        return NotFound();
                    }

                    if (plan.Status != RoutePlanStatuses.Draft)
                    {
                        await transaction.RollbackAsync();

                        TempData["Error"] =
                            "Only DRAFT route plans can be edited.";

                        return RedirectToAction(
                            nameof(Details),
                            new { id = plan.Id });
                    }

                    // Replace the stop snapshot for this DRAFT plan.
                    _context.RoutePlanStops.RemoveRange(plan.Stops);
                }

                // -------------------------------------------------
                // 6. Save header
                // -------------------------------------------------
                plan.PlanDate = vm.PlanDate.Date;
                plan.RouteId = route.ID;
                plan.RouteCode = route.ROUTE_CODE;
                plan.RouteName = route.ROUTE_NAME;

                plan.CustomerCode =
                    string.IsNullOrWhiteSpace(vm.CustomerCode)
                        ? null
                        : vm.CustomerCode.Trim();

                plan.PlannedDeparture = vm.PlannedDeparture;

                plan.VehicleNo =
                    string.IsNullOrWhiteSpace(vm.VehicleNo)
                        ? null
                        : vm.VehicleNo.Trim();

                plan.DriverName =
                    string.IsNullOrWhiteSpace(vm.DriverName)
                        ? null
                        : vm.DriverName.Trim();

                plan.Notes =
                    string.IsNullOrWhiteSpace(vm.Notes)
                        ? null
                        : vm.Notes.Trim();

                plan.UpdatedBy = username;
                plan.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // -------------------------------------------------
                // 7. Snapshot Master Route stops
                //
                // Route Plan retains the location data at the
                // time of saving, even if the master changes later.
                // -------------------------------------------------
                foreach (var stop in masterStops)
                {
                    _context.RoutePlanStops.Add(new RoutePlanStop
                    {
                        RoutePlanId = plan.Id,
                        MasterRouteStopId = stop.ID,
                        GeofenceTableId = stop.GEOFENCE_TABLE_ID,
                        SequenceNo = stop.SEQUENCE_NO,
                        StopType = stop.STOP_TYPE,
                        FenceName = stop.FenceName ?? "(Unnamed Location)",
                        Address = stop.Address,
                        City = stop.City,
                        PlannedArrival = stop.PLANNED_ARRIVAL,
                        //StopDurationMinutes = stop.STOP_DURATION_MINUTES,
                        StopDurationMinutes = stop.STOP_DURATION_MINUTES ?? 0,
                        Notes = stop.NOTES,
                        Status = RoutePlanStopStatuses.Pending
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] =
                    $"Route Plan {plan.PlanNo} saved as DRAFT.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = plan.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to save Route Plan {PlanId}",
                    vm.Id);

                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to save the Route Plan. Check the application log for details.");

                await RepopulateFormAsync(vm);

                return View("Form", vm);
            }
        }

        // =========================================================
        // AJAX: CUSTOMERS ASSIGNED TO A MASTER ROUTE
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetRouteCustomers(int routeId)
        {
            if (routeId <= 0)
            {
                return Json(new List<object>());
            }

            // CustomerCode matches CustomerGroup.CUST_CODE.
            var codes = await _context.RouteCustomerAssignments
                .AsNoTracking()
                .Where(x => x.ROUTE_ID == routeId)
                .Select(x => x.CUSTOMER_CODE)
                .Distinct()
                .ToListAsync();

            var customers = await _context.CustomerGroups
                .AsNoTracking()
                .Where(x =>
                    x.CUST_CODE != null &&
                    codes.Contains(x.CUST_CODE))
                .OrderBy(x => x.CUST_CODE)
                .Select(x => new
                {
                    value = x.CUST_CODE,
                    text = x.CUST_CODE
                })
                .Distinct()
                .ToListAsync();

            return Json(customers);
        }

        // =========================================================
        // AJAX: MASTER ROUTE STOPS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetRouteStops(int routeId)
        {
            if (routeId <= 0)
            {
                return Json(new List<object>());
            }

            var stops = await (
                from rs in _context.RouteStops.AsNoTracking()
                join g in _context.Geofences.AsNoTracking()
                    on rs.GEOFENCE_TABLE_ID equals g.Id
                where rs.ROUTE_ID == routeId
                orderby rs.SEQUENCE_NO
                select new
                {
                    rs.SEQUENCE_NO,
                    rs.STOP_TYPE,
                    FenceName = g.FenceName ?? "(Unnamed Location)",
                    g.Address,
                    g.City,
                    PlannedArrival = rs.PLANNED_ARRIVAL.HasValue
                        ? rs.PLANNED_ARRIVAL.Value.ToString(@"hh\:mm")
                        : null,
                    rs.STOP_DURATION_MINUTES
                })
                .ToListAsync();

            return Json(stops);
        }

        // =========================================================
        // ROUTE PLAN DETAILS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var plan = await _context.RoutePlans
                .AsNoTracking()
                .Include(x => x.Stops.OrderBy(s => s.SequenceNo))
                    .ThenInclude(s => s.History
                        .OrderByDescending(h => h.ChangedAt))
                .FirstOrDefaultAsync(x => x.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // =========================================================
        // CONFIRM PLAN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPlan(int id)
        {
            var plan = await _context.RoutePlans
                .Include(x => x.Stops)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            if (plan.Status != RoutePlanStatuses.Draft)
            {
                return BadRequest("Only DRAFT plans can be confirmed.");
            }

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(plan.VehicleNo))
            {
                errors.Add("Vehicle is required.");
            }

            if (string.IsNullOrWhiteSpace(plan.DriverName))
            {
                errors.Add("Driver is required.");
            }

            if (!plan.PlannedDeparture.HasValue)
            {
                errors.Add("Planned departure is required.");
            }

            if (plan.Stops.Count == 0)
            {
                errors.Add("At least one stop is required.");
            }

            if (errors.Count > 0)
            {
                TempData["Error"] = string.Join(" ", errors);

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var username =
                HttpContext.Session.GetString("username") ?? "System";

            plan.Status = RoutePlanStatuses.Confirmed;
            plan.ConfirmedBy = username;
            plan.ConfirmedAt = DateTime.Now;
            plan.UpdatedBy = username;
            plan.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Route Plan confirmed.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // START ROUTE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartRoute(int id)
        {
            var plan = await _context.RoutePlans
                .FirstOrDefaultAsync(x => x.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            if (plan.Status != RoutePlanStatuses.Confirmed)
            {
                return BadRequest(
                    "Only CONFIRMED plans can be started.");
            }

            var username =
                HttpContext.Session.GetString("username") ?? "System";

            plan.Status = RoutePlanStatuses.InProgress;
            plan.StartedBy = username;
            plan.StartedAt = DateTime.Now;
            plan.UpdatedBy = username;
            plan.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Route started.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // UPDATE STOP STATUS
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStopStatus(
            RoutePlanStopStatusInput input)
        {
            var stop = await _context.RoutePlanStops
                .Include(x => x.RoutePlan)
                .FirstOrDefaultAsync(x => x.Id == input.StopId);

            if (stop == null || stop.RoutePlan == null)
            {
                return NotFound();
            }

            if (stop.RoutePlan.Status != RoutePlanStatuses.InProgress)
            {
                return BadRequest(
                    "Stop statuses can only be updated while the Route Plan is IN_PROGRESS.");
            }

            var validTransition =
                (stop.Status == RoutePlanStopStatuses.Pending &&
                 input.NewStatus == RoutePlanStopStatuses.Arrived)
                ||
                (stop.Status == RoutePlanStopStatuses.Arrived &&
                 input.NewStatus == RoutePlanStopStatuses.Completed);

            if (!validTransition)
            {
                return BadRequest(
                    "Invalid stop status transition. Use PENDING → ARRIVED → COMPLETED.");
            }

            var username =
                HttpContext.Session.GetString("username") ?? "System";

            var oldStatus = stop.Status;
            var now = DateTime.Now;

            if (input.NewStatus == RoutePlanStopStatuses.Arrived)
            {
                stop.ActualArrivalAt = now;
            }
            else if (input.NewStatus == RoutePlanStopStatuses.Completed)
            {
                stop.CompletedAt = now;
            }

            stop.Status = input.NewStatus;

            _context.RoutePlanStopHistories.Add(
                new RoutePlanStopHistory
                {
                    RoutePlanId = stop.RoutePlanId,
                    RoutePlanStopId = stop.Id,
                    PreviousStatus = oldStatus,
                    NewStatus = input.NewStatus,
                    ActionType = "STATUS_CHANGE",
                    ChangedBy = username,
                    ChangedAt = now
                });

            stop.RoutePlan.UpdatedBy = username;
            stop.RoutePlan.UpdatedAt = now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Stop {stop.SequenceNo} updated to {input.NewStatus}.";

            return RedirectToAction(
                nameof(Details),
                new { id = stop.RoutePlanId });
        }

        // =========================================================
        // CORRECT STOP STATUS
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CorrectStopStatus(
            RoutePlanStopCorrectionInput input)
        {
            // Configure this session flag in the existing
            // authorization/login flow.
            if (!string.Equals(
                HttpContext.Session.GetString("RoutePlanCanCorrectStop"),
                "true",
                StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (!RoutePlanStopStatuses.All.Contains(input.NewStatus))
            {
                return BadRequest("Invalid stop status.");
            }

            if (string.IsNullOrWhiteSpace(input.Reason))
            {
                return BadRequest("Correction reason is required.");
            }

            var stop = await _context.RoutePlanStops
                .Include(x => x.RoutePlan)
                .FirstOrDefaultAsync(x => x.Id == input.StopId);

            if (stop == null || stop.RoutePlan == null)
            {
                return NotFound();
            }

            if (stop.RoutePlan.Status != RoutePlanStatuses.InProgress)
            {
                return BadRequest(
                    "Stop correction is allowed only while the Route Plan is IN_PROGRESS.");
            }

            var username =
                HttpContext.Session.GetString("username") ?? "System";

            var oldStatus = stop.Status;
            var now = DateTime.Now;

            stop.Status = input.NewStatus;

            stop.ActualArrivalAt =
                input.NewStatus == RoutePlanStopStatuses.Pending
                    ? null
                    : stop.ActualArrivalAt ?? now;

            stop.CompletedAt =
                input.NewStatus == RoutePlanStopStatuses.Completed
                    ? stop.CompletedAt ?? now
                    : null;

            _context.RoutePlanStopHistories.Add(
                new RoutePlanStopHistory
                {
                    RoutePlanId = stop.RoutePlanId,
                    RoutePlanStopId = stop.Id,
                    PreviousStatus = oldStatus,
                    NewStatus = input.NewStatus,
                    ActionType = "CORRECTION",
                    Reason = input.Reason.Trim(),
                    ChangedBy = username,
                    ChangedAt = now
                });

            stop.RoutePlan.UpdatedBy = username;
            stop.RoutePlan.UpdatedAt = now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Stop status corrected and audit history recorded.";

            return RedirectToAction(
                nameof(Details),
                new { id = stop.RoutePlanId });
        }

        // =========================================================
        // COMPLETE ROUTE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRoute(int id)
        {
            var plan = await _context.RoutePlans
                .Include(x => x.Stops)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            if (plan.Status != RoutePlanStatuses.InProgress)
            {
                return BadRequest(
                    "Only IN_PROGRESS plans can be completed.");
            }

            if (plan.Stops.Count == 0 ||
                plan.Stops.Any(s =>
                    s.Status != RoutePlanStopStatuses.Completed))
            {
                TempData["Error"] =
                    "All stops must be COMPLETED before completing the Route Plan.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var username =
                HttpContext.Session.GetString("username") ?? "System";

            plan.Status = RoutePlanStatuses.Completed;
            plan.CompletedBy = username;
            plan.CompletedAt = DateTime.Now;
            plan.UpdatedBy = username;
            plan.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Route Plan completed.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // CANCEL PLAN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelPlan(int id)
        {
            var plan = await _context.RoutePlans
                .FirstOrDefaultAsync(x => x.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            if (plan.Status != RoutePlanStatuses.Draft &&
                plan.Status != RoutePlanStatuses.Confirmed)
            {
                return BadRequest(
                    "Only DRAFT or CONFIRMED plans can be cancelled.");
            }

            var username =
                HttpContext.Session.GetString("username") ?? "System";

            plan.Status = RoutePlanStatuses.Cancelled;
            plan.UpdatedBy = username;
            plan.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Route Plan cancelled.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // DROPDOWN: ACTIVE MASTER ROUTES
        // =========================================================
        private async Task PopulateRoutes(RoutePlanFormViewModel vm)
        {
            vm.Routes = await _context.Routes
                .AsNoTracking()
                .Where(x => x.STATUS == "ACTIVE")
                .OrderBy(x => x.ROUTE_CODE)
                .Select(x => new SelectListItem
                {
                    Value = x.ID.ToString(),
                    Text = x.ROUTE_CODE + " - " + x.ROUTE_NAME,
                    Selected = vm.RouteId == x.ID
                })
                .ToListAsync();
        }

        // =========================================================
        // DROPDOWN: CUSTOMERS ASSIGNED TO MASTER ROUTE
        // =========================================================
        private async Task PopulateCustomers(
            RoutePlanFormViewModel vm,
            int routeId)
        {
            var assignedCodes = await _context.RouteCustomerAssignments
                .AsNoTracking()
                .Where(x => x.ROUTE_ID == routeId)
                .Select(x => x.CUSTOMER_CODE)
                .Distinct()
                .ToListAsync();

            vm.Customers = await _context.CustomerGroups
                .AsNoTracking()
                .Where(x =>
                    x.CUST_CODE != null &&
                    assignedCodes.Contains(x.CUST_CODE))
                .OrderBy(x => x.CUST_CODE)
                .Select(x => new SelectListItem
                {
                    Value = x.CUST_CODE,
                    Text = x.CUST_CODE,
                    Selected = vm.CustomerCode == x.CUST_CODE
                })
                .Distinct()
                .ToListAsync();
        }

        // =========================================================
        // REPOPULATE DROPDOWNS AFTER VALIDATION FAILURE
        // =========================================================
        private async Task RepopulateFormAsync(
            RoutePlanFormViewModel vm)
        {
            await PopulateRoutes(vm);

            if (vm.RouteId.HasValue && vm.RouteId.Value > 0)
            {
                await PopulateCustomers(vm, vm.RouteId.Value);
            }
            else
            {
                vm.Customers = new List<SelectListItem>();
            }
        }

        // =========================================================
        // GENERATE PLAN NUMBER
        // =========================================================
        private async Task<string> GeneratePlanNoAsync()
        {
            var prefix = "RP-" + DateTime.Now.ToString("yyyyMMdd") + "-";

            var last = await _context.RoutePlans
                .Where(x => x.PlanNo.StartsWith(prefix))
                .OrderByDescending(x => x.PlanNo)
                .Select(x => x.PlanNo)
                .FirstOrDefaultAsync();

            var next = 1;

            if (!string.IsNullOrWhiteSpace(last) &&
                last.Length > prefix.Length)
            {
                var suffix = last.Substring(prefix.Length);

                if (int.TryParse(suffix, out var parsed))
                {
                    next = parsed + 1;
                }
            }

            return prefix + next.ToString("D3");
        }
    }
}
