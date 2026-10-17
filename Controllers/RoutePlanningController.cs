
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Models.ViewModels;
using System.Net.Http;
using System.Globalization;
using System.Text.Json;
using TMSBilling.Services;

namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class RoutePlanningController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RoutePlanningController> _logger;
        private readonly ApiService _apiService;

        // Cache driver data briefly because the external API is paginated.
        private static readonly SemaphoreSlim _driverCacheLock = new(1, 1);
        private static List<ExternalDriver>? _cachedDrivers;
        private static DateTime _cachedDriversExpiresAtUtc = DateTime.MinValue;
        private static readonly TimeSpan DriverCacheDuration = TimeSpan.FromMinutes(10);


        public RoutePlanningController(
        AppDbContext context,
        ILogger<RoutePlanningController> logger,
        ApiService apiService)
        {
            _context = context;
            _logger = logger;
            _apiService = apiService;
        }

        //public RoutePlanningController(
        //    AppDbContext context,
        //    ILogger<RoutePlanningController> logger)
        //{
        //    _context = context;
        //    _logger = logger;
        //}

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
                vm.VehicleId = plan.VehicleId;
                vm.DriverName = plan.DriverName;
                vm.DriverId = plan.DriverId;
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
                        PlannedDeparture = s.PlannedDeparture,
                        StopDurationMinutes = s.StopDurationMinutes,
                        Status = s.Status,
                        ActualArrivalAt = s.ActualArrivalAt,
                        ActualDepartureAt = s.ActualDepartureAt,
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

            await PopulateVehiclesAsync(vm);
            await PopulateDriversAsync(vm);

            return View(vm);
        }

        // =========================================================
        // CREATE / EDIT FORM
        // =========================================================
        //[HttpGet]
        //public async Task<IActionResult> Form(int? id)
        //{
        //    var vm = new RoutePlanFormViewModel
        //    {
        //        PlanDate = DateTime.Today,
        //        Status = RoutePlanStatuses.Draft
        //    };

        //    if (id.HasValue)
        //    {
        //        var plan = await _context.RoutePlans
        //            .AsNoTracking()
        //            .Include(x => x.Stops)
        //            .FirstOrDefaultAsync(x => x.Id == id.Value);

        //        if (plan == null)
        //        {
        //            return NotFound();
        //        }

        //        if (plan.Status != RoutePlanStatuses.Draft)
        //        {
        //            TempData["Error"] =
        //                "Only DRAFT route plans can be edited.";

        //            return RedirectToAction(
        //                nameof(Details),
        //                new { id = plan.Id });
        //        }

        //        vm.Id = plan.Id;
        //        vm.PlanNo = plan.PlanNo;
        //        vm.PlanDate = plan.PlanDate;
        //        vm.RouteId = plan.RouteId;
        //        vm.CustomerCode = plan.CustomerCode;
        //        vm.PlannedDeparture = plan.PlannedDeparture;
        //        vm.VehicleNo = plan.VehicleNo;
        //        vm.VehicleId = plan.VehicleId;
        //        vm.DriverName = plan.DriverName;
        //        vm.DriverId = plan.DriverId;
        //        vm.Notes = plan.Notes;
        //        vm.Status = plan.Status;

        //        vm.Stops = plan.Stops
        //            .OrderBy(s => s.SequenceNo)
        //            .Select(s => new RoutePlanStopPreviewViewModel
        //            {
        //                StopId = s.Id,
        //                SequenceNo = s.SequenceNo,
        //                StopType = s.StopType,
        //                FenceName = s.FenceName,
        //                Address = s.Address,
        //                City = s.City,
        //                PlannedArrival = s.PlannedArrival,
        //                StopDurationMinutes = s.StopDurationMinutes,
        //                Status = s.Status,
        //                ActualArrivalAt = s.ActualArrivalAt,
        //                CompletedAt = s.CompletedAt
        //            })
        //            .ToList();
        //    }

        //    await PopulateRoutes(vm);

        //    if (vm.RouteId.HasValue)
        //    {
        //        await PopulateCustomers(vm, vm.RouteId.Value);
        //    }
        //    else
        //    {
        //        vm.Customers = new List<SelectListItem>();
        //    }

        //    await PopulateVehiclesAsync(vm);
        //    await PopulateDriversAsync(vm);

        //    return View(vm);
        //}


        // =========================================================
        // SAVE ROUTE PLAN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(RoutePlanFormViewModel vm)
        {
            var username = HttpContext.Session.GetString("username") ?? "System";

            if (vm.Id == 0)
                ModelState.Remove(nameof(vm.PlanNo));
            ModelState.Remove(nameof(vm.Status));

            // 1. Basic validation
            if (!vm.RouteId.HasValue || vm.RouteId.Value <= 0)
                ModelState.AddModelError(nameof(vm.RouteId), "Please select a Master Route.");

            if (vm.PlanDate == default)
                ModelState.AddModelError(nameof(vm.PlanDate), "Plan Date is required.");

            // Planned Departure is the scheduling key; PlanDate is not part of
            // duplicate detection. The UI datetime-local control has minute precision,
            // so normalize the submitted value to the same precision.
            if (!vm.PlannedDeparture.HasValue)
            {
                ModelState.AddModelError(
                    nameof(vm.PlannedDeparture),
                    "Planned Departure is required to schedule a Route Plan cycle.");
            }
            else
            {
                var requestedDeparture = vm.PlannedDeparture.Value;
                vm.PlannedDeparture = new DateTime(
                    requestedDeparture.Year,
                    requestedDeparture.Month,
                    requestedDeparture.Day,
                    requestedDeparture.Hour,
                    requestedDeparture.Minute,
                    0,
                    requestedDeparture.Kind);
            }

            if (!ModelState.IsValid)
            {
                LogModelStateErrors();
                await RepopulateFormAsync(vm);
                return View("Form", vm);
            }

            // 2. Load active Master Route
            var route = await _context.Routes
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ID == vm.RouteId!.Value && r.STATUS == "ACTIVE");

            if (route == null)
            {
                ModelState.AddModelError(nameof(vm.RouteId), "The selected route does not exist or is inactive.");
                await RepopulateFormAsync(vm);
                return View("Form", vm);
            }

            // 3. Load current Master Route stops
            var masterStops = await (
                from rs in _context.RouteStops
                join g in _context.Geofences on rs.GEOFENCE_TABLE_ID equals g.Id
                where rs.ROUTE_ID == route.ID
                orderby rs.SEQUENCE_NO
                select new
                {
                    rs.ID,
                    rs.GEOFENCE_TABLE_ID,
                    rs.SEQUENCE_NO,
                    rs.STOP_TYPE,
                    rs.PLANNED_ARRIVAL,
                    rs.PLANNED_DEPARTURE,
                    rs.STOP_DURATION_MINUTES,
                    rs.NOTES,
                    g.FenceName,
                    g.Address,
                    g.City,
                    g.Lat,
                    g.Long,
                    g.PhoneNo
                })
                .ToListAsync();

            if (masterStops.Count == 0)
            {
                ModelState.AddModelError(nameof(vm.RouteId), "The selected Master Route has no valid stops.");
                await RepopulateFormAsync(vm);
                return View("Form", vm);
            }

            // 4. Validate customer assignment to the selected Master Route
            var assignedCustomerCodes = await _context.RouteCustomerAssignments
                .AsNoTracking()
                .Where(x => x.ROUTE_ID == route.ID)
                .Select(x => x.CUSTOMER_CODE)
                .Distinct()
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(vm.CustomerCode))
            {
                vm.CustomerCode = vm.CustomerCode.Trim();
                var customerAssigned = assignedCustomerCodes.Any(code =>
                    string.Equals(code, vm.CustomerCode, StringComparison.OrdinalIgnoreCase));

                if (!customerAssigned)
                {
                    ModelState.AddModelError(nameof(vm.CustomerCode),
                        "The selected customer is not assigned to this Master Route.");
                    await RepopulateFormAsync(vm);
                    return View("Form", vm);
                }
            }

            // 5. Validate external vehicle and driver
            ExternalVehicle? selectedVehicle = null;
            ExternalDriver? selectedDriver = null;

            try
            {
                if (vm.VehicleId.HasValue && vm.VehicleId.Value > 0)
                {
                    var vehicles = await FetchVehiclesFromApiAsync();
                    selectedVehicle = vehicles.FirstOrDefault(v => v.Id == vm.VehicleId.Value);
                    if (selectedVehicle == null)
                        ModelState.AddModelError(nameof(vm.VehicleId),
                            "The selected vehicle is no longer available in the external fleet system. Please select it again.");
                }

                if (vm.DriverId.HasValue && vm.DriverId.Value > 0)
                {
                    var drivers = await FetchDriversFromApiAsync();
                    selectedDriver = drivers.FirstOrDefault(d => d.Id == vm.DriverId.Value);
                    if (selectedDriver == null)
                        ModelState.AddModelError(nameof(vm.DriverId),
                            "The selected driver is no longer available in the external fleet system. Please select them again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate external vehicle/driver selection.");
                ModelState.AddModelError(string.Empty,
                    "Unable to validate the selected vehicle or driver against the external fleet system. Please retry.");
            }

            if (!ModelState.IsValid)
            {
                await RepopulateFormAsync(vm);
                return View("Form", vm);
            }

            // 6. Load and validate the existing local DRAFT, if this is an edit
            RoutePlanHeader? existingDraftPlan = null;
            if (vm.Id > 0)
            {
                existingDraftPlan = await _context.RoutePlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == vm.Id);

                if (existingDraftPlan == null)
                    return NotFound();

                if (existingDraftPlan.Status != RoutePlanStatuses.Draft)
                {
                    TempData["Error"] = "Only DRAFT route plans can be edited.";
                    return RedirectToAction(nameof(Details), new { id = existingDraftPlan.Id });
                }

                if (string.IsNullOrWhiteSpace(existingDraftPlan.ExternalFleetTaskId))
                {
                    ModelState.AddModelError(string.Empty,
                        "External Fleet Task ID is missing. Data was not saved to avoid a local/external mismatch.");
                    await RepopulateFormAsync(vm);
                    return View("Form", vm);
                }
            }

            if (selectedVehicle == null || selectedDriver == null)
            {
                if (selectedVehicle == null)
                    ModelState.AddModelError(nameof(vm.VehicleId), "Select a valid external vehicle.");
                if (selectedDriver == null)
                    ModelState.AddModelError(nameof(vm.DriverId), "Select a valid external driver.");
                await RepopulateFormAsync(vm);
                return View("Form", vm);
            }

            // STRICT duplicate key: Route + Planned Departure (minute) + Vehicle ID + Driver ID.
            // PlanDate is deliberately not part of this rule. Other cycles can be planned
            // in advance, including with the same vehicle/driver at a different departure.
            // Exclude the current plan on UPDATE so it does not collide with itself.
            var departureMinute = vm.PlannedDeparture!.Value;
            var departureMinuteEnd = departureMinute.AddMinutes(1);

            var duplicatePlan = await _context.RoutePlans
                .AsNoTracking()
                .Where(x =>
                    x.Id != vm.Id &&
                    x.RouteId == route.ID &&
                    x.VehicleId == selectedVehicle.Id &&
                    x.DriverId == selectedDriver.Id &&
                    x.PlannedDeparture.HasValue &&
                    x.PlannedDeparture.Value >= departureMinute &&
                    x.PlannedDeparture.Value < departureMinuteEnd)
                .Select(x => new { x.PlanNo })
                .FirstOrDefaultAsync();

            if (duplicatePlan != null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"A Route Plan already exists with the same Route, Planned Departure, Vehicle, and Driver (Plan No: {duplicatePlan.PlanNo}). Change at least one of these values.");
                await RepopulateFormAsync(vm);
                return View("Form", vm);
            }

            var generatedPlanNo = vm.Id == 0
                ? await GeneratePlanNoAsync()
                : existingDraftPlan!.PlanNo;
            var externalFleetTaskId = existingDraftPlan?.ExternalFleetTaskId;

            // Temporary defaults retained from the currently working API integration.
            const int debugUserGroupId = 1;
            const string debugExternalAddressId = "1";
            const double debugLatitude = -7.265122697218518;
            const double debugLongitude = 112.74688141009835;

            var generalNote = string.IsNullOrWhiteSpace(vm.Notes) ? "mendesak" : vm.Notes.Trim();

            // Build the target points once. The same mapping is used for initial CREATE
            // and for one-by-one point PATCH/POST during UPDATE.
            var updatePointPayloads = masterStops
                .Select((stop, index) =>
                {
                    var locationName = string.IsNullOrWhiteSpace(stop.FenceName)
                        ? "McEasy Surabaya"
                        : stop.FenceName.Trim();
                    var fullAddress = string.IsNullOrWhiteSpace(stop.Address)
                        ? locationName
                        : stop.Address.Trim();
                    var lat = ParseCoordinateOrFallback(stop.Lat, debugLatitude);
                    var lng = ParseCoordinateOrFallback(stop.Long, debugLongitude);
                    var durationMinutes = stop.STOP_DURATION_MINUTES.HasValue && stop.STOP_DURATION_MINUTES.Value > 0
                        ? stop.STOP_DURATION_MINUTES.Value
                        : 15;
                    var targetArrival = ResolveTargetArrival(
                        vm.PlanDate,
                        vm.PlannedDeparture,
                        stop.PLANNED_ARRIVAL,
                        stop.SEQUENCE_NO);
                    var targetDeparture = targetArrival.AddMinutes(durationMinutes);
                    var stopNote = string.IsNullOrWhiteSpace(stop.NOTES) ? generalNote : stop.NOTES.Trim();

                    // Preserve the currently working activity label until official mapping is finalized.
                    return new Dictionary<string, object?>
                    {
                        //["activity_label"] = "PICKUP",
                        ["activity_label"] = MapExternalActivityLabel(stop.STOP_TYPE),
                        ["address"] = new Dictionary<string, object?>
                        {
                            ["full_name"] = fullAddress,
                            ["id"] = debugExternalAddressId,
                            ["lat"] = lat,
                            ["lng"] = lng,
                            ["name"] = locationName,
                            ["type"] = "GOOGLE_MAP"
                        },
                        ["note"] = stopNote,
                        ["plan_sequence"] = index + 1,
                        ["service_time"] = $"PT{durationMinutes}M",
                        ["target_time_arrival"] = FormatJakartaDateTime(targetArrival),
                        ["target_time_departure"] = FormatJakartaDateTime(targetDeparture),
                        ["type"] = "DEFAULT"
                    };
                })
                .ToList();

            // The CREATE endpoint accepts point payloads without plan_sequence.
            var createPointPayloads = updatePointPayloads
                .Select(point =>
                {
                    var copy = new Dictionary<string, object?>(point);
                    copy.Remove("plan_sequence");
                    return copy;
                })
                .ToList();

            var phoneNumber = string.IsNullOrWhiteSpace(selectedDriver.PhoneNo)
                ? "081234567890"
                : selectedDriver.PhoneNo;

            var createFleetTaskPayload = new
            {
                custom_field = new Dictionary<string, string> { ["key"] = "value" },
                driver_id = selectedDriver.Id,
                note = $"Route Plan {generatedPlanNo} - {generalNote}",
                phone_number = phoneNumber,
                points = createPointPayloads,
                send_late_notification = true,
                user_group_id = debugUserGroupId,
                vehicle_id = selectedVehicle.Id
            };

            // UPDATE: the external API is currently being called one header field at a time.
            // Keep this list limited to the three confirmed fields requested for now.
            // They are sent sequentially on every Save so retries can safely reapply them.
            var updateHeaderFieldPayloads = new List<(string FieldName, Dictionary<string, object?> Payload)>
            {
                ("driver_id", new Dictionary<string, object?>
                {
                    ["driver_id"] = selectedDriver.Id
                }),
                ("vehicle_id", new Dictionary<string, object?>
                {
                    ["vehicle_id"] = selectedVehicle.Id
                }),
                ("note", new Dictionary<string, object?>
                {
                    ["note"] = $"Route Plan {generatedPlanNo} - {generalNote}"
                })
            };

            try
            {
                if (vm.Id == 0)
                {
                    // CREATE: preserve the existing working flow. Create task -> transition
                    // to DRAFT -> only then create the local record. This is not an edit.
                    var createPayloadLog = JsonSerializer.Serialize(createFleetTaskPayload);
                    _logger.LogInformation(
                        "Creating external fleet task. PlanNo={PlanNo}; Payload={Payload}",
                        generatedPlanNo, createPayloadLog);

                    var (createOk, createJson) = await _apiService.SendRequestAsync(
                        HttpMethod.Post,
                        "fleet-planning/api/web/v1/fleet-task-instant",
                        createFleetTaskPayload);

                    var createResponseText = createJson.ToString();
                    _logger.LogInformation(
                        "Create external fleet task response. PlanNo={PlanNo}; Success={Success}; Response={Response}",
                        generatedPlanNo, createOk, createResponseText);

                    if (!createOk)
                    {
                        ModelState.AddModelError(string.Empty,
                            "External fleet task gagal dibuat. Respons API: " + createResponseText);
                        await RepopulateFormAsync(vm);
                        return View("Form", vm);
                    }

                    externalFleetTaskId = TryReadExternalFleetTaskId(createJson);
                    if (string.IsNullOrWhiteSpace(externalFleetTaskId))
                    {
                        _logger.LogError(
                            "External task creation returned no task ID. PlanNo={PlanNo}; Response={Response}",
                            generatedPlanNo, createResponseText);
                        ModelState.AddModelError(string.Empty,
                            "API eksternal merespons sukses tetapi ID Fleet Task tidak ditemukan. Data lokal belum disimpan untuk mencegah duplikasi. Periksa log/response API: " + createResponseText);
                        await RepopulateFormAsync(vm);
                        return View("Form", vm);
                    }

                    var (transitionOk, transitionJson) = await _apiService.SendRequestAsync(
                        HttpMethod.Post,
                        $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(externalFleetTaskId)}/transition",
                        new { status = "DRAFT" });

                    var transitionResponseText = transitionJson.ToString();
                    _logger.LogInformation(
                        "Initial external DRAFT transition. ExternalFleetTaskId={ExternalFleetTaskId}; Success={Success}; Response={Response}",
                        externalFleetTaskId, transitionOk, transitionResponseText);

                    if (!transitionOk)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"External Fleet Task sudah terbentuk dengan ID {externalFleetTaskId}, tetapi transition awal ke DRAFT gagal. Data lokal belum disimpan. Jangan submit Create ulang karena dapat membuat task duplikat. Respons: {transitionResponseText}");
                        await RepopulateFormAsync(vm);
                        return View("Form", vm);
                    }
                }
                else
                {
                    // UPDATE: read the current planned point IDs before changing anything.
                    // We only know how to synchronize DEFAULT points from Master Route. If
                    // external START/END points exist, stop safely rather than deleting or
                    // overwriting points that do not have a local Master Route equivalent.
                    var readResult = await ReadExternalPointsAsync(externalFleetTaskId!);
                    if (!readResult.Success)
                    {
                        ModelState.AddModelError(string.Empty,
                            "Tidak dapat membaca/validasi point eksternal. Header dan data lokal belum diubah. " + readResult.Response);
                        await RepopulateFormAsync(vm);
                        return View("Form", vm);
                    }

                    var existingExternalPoints = readResult.Points
                        .OrderBy(point => point.Sequence)
                        .ToList();

                    // 1) PATCH the confirmed header fields individually and sequentially.
                    // No /transition call is made during Save. If one request fails, stop before
                    // touching points/local DB. Earlier successful field PATCHes may remain in
                    // the external system; retrying Save reapplies all three fields idempotently.
                    var headerEndpoint =
                        $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(externalFleetTaskId!)}";

                    foreach (var headerField in updateHeaderFieldPayloads)
                    {
                        var headerPayloadLog = JsonSerializer.Serialize(headerField.Payload);
                        _logger.LogInformation(
                            "Patching external Fleet Task header field. PlanNo={PlanNo}; ExternalFleetTaskId={ExternalFleetTaskId}; Field={Field}; Payload={Payload}",
                            generatedPlanNo, externalFleetTaskId, headerField.FieldName, headerPayloadLog);

                        var (headerOk, headerJson) = await _apiService.SendRequestAsync(
                            HttpMethod.Patch,
                            headerEndpoint,
                            headerField.Payload);

                        var headerResponseText = headerJson.ToString();
                        _logger.LogInformation(
                            "External Fleet Task header field PATCH response. PlanNo={PlanNo}; ExternalFleetTaskId={ExternalFleetTaskId}; Field={Field}; Success={Success}; Response={Response}",
                            generatedPlanNo, externalFleetTaskId, headerField.FieldName, headerOk, headerResponseText);

                        if (!headerOk)
                        {
                            ModelState.AddModelError(string.Empty,
                                $"Update header field '{headerField.FieldName}' ke sistem eksternal gagal. Field sebelumnya mungkin sudah berhasil di-update, tetapi points dan database lokal belum diubah. Save ulang akan mengirim kembali driver_id, vehicle_id, dan note satu per satu. Respons API: {headerResponseText}");
                            await RepopulateFormAsync(vm);
                            return View("Form", vm);
                        }
                    }

                    // 2) PATCH the points that remain, matching by planned sequence.
                    // Since both lists are sorted by sequence, the point ID is retained while
                    // the stop data and desired plan_sequence are updated.
                    var commonCount = Math.Min(existingExternalPoints.Count, updatePointPayloads.Count);
                    for (var index = 0; index < commonCount; index++)
                    {
                        var externalPoint = existingExternalPoints[index];
                        var pointEndpoint =
                            $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(externalFleetTaskId!)}/point/{Uri.EscapeDataString(externalPoint.Id)}";
                        var pointPayloadLog = JsonSerializer.Serialize(updatePointPayloads[index]);

                        _logger.LogInformation(
                            "Patching external point. PlanNo={PlanNo}; PointId={PointId}; OldSequence={OldSequence}; NewSequence={NewSequence}; Payload={Payload}",
                            generatedPlanNo, externalPoint.Id, externalPoint.Sequence, index + 1, pointPayloadLog);

                        var (pointPatchOk, pointPatchJson) = await _apiService.SendRequestAsync(
                            HttpMethod.Patch,
                            pointEndpoint,
                            updatePointPayloads[index]);

                        var pointPatchResponseText = pointPatchJson.ToString();
                        _logger.LogInformation(
                            "External point PATCH response. PlanNo={PlanNo}; PointId={PointId}; Success={Success}; Response={Response}",
                            generatedPlanNo, externalPoint.Id, pointPatchOk, pointPatchResponseText);

                        if (!pointPatchOk)
                        {
                            ModelState.AddModelError(string.Empty,
                                $"Sinkronisasi point gagal pada urutan {index + 1} (ID {externalPoint.Id}). Header atau point sebelumnya mungkin sudah berhasil berubah di eksternal, tetapi database lokal belum disimpan. Save ulang akan membaca daftar point terbaru lalu melanjutkan sinkronisasi. Respons API: {pointPatchResponseText}");
                            await RepopulateFormAsync(vm);
                            return View("Form", vm);
                        }
                    }

                    // 3) If there are extra old points, delete highest sequence first.
                    // This avoids deleting a point still needed by the target list and follows
                    // the sequential order required by the API.
                    if (existingExternalPoints.Count > updatePointPayloads.Count)
                    {
                        var pointsToDelete = existingExternalPoints
                            .Skip(updatePointPayloads.Count)
                            .OrderByDescending(point => point.Sequence)
                            .ToList();

                        foreach (var point in pointsToDelete)
                        {
                            var deleteEndpoint =
                                $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(externalFleetTaskId!)}/point/{Uri.EscapeDataString(point.Id)}";

                            _logger.LogInformation(
                                "Deleting excess external point. PlanNo={PlanNo}; PointId={PointId}; Sequence={Sequence}",
                                generatedPlanNo, point.Id, point.Sequence);

                            var (deleteOk, deleteJson) = await _apiService.SendRequestAsync(
                                HttpMethod.Delete,
                                deleteEndpoint);

                            var deleteResponseText = deleteJson.ToString();
                            _logger.LogInformation(
                                "External point DELETE response. PlanNo={PlanNo}; PointId={PointId}; Success={Success}; Response={Response}",
                                generatedPlanNo, point.Id, deleteOk, deleteResponseText);

                            if (!deleteOk)
                            {
                                ModelState.AddModelError(string.Empty,
                                    $"Gagal menghapus point eksternal urutan {point.Sequence} (ID {point.Id}). Sebagian perubahan eksternal mungkin sudah berhasil, tetapi database lokal belum disimpan. Save ulang akan membaca ulang point terbaru. Respons API: {deleteResponseText}");
                                await RepopulateFormAsync(vm);
                                return View("Form", vm);
                            }
                        }
                    }
                    // 4) If new points are needed, POST each one sequentially. The POST
                    // schema supplied does not accept plan_sequence, so request order is used;
                    // the final GET below verifies the sequence assigned by the API.
                    else if (updatePointPayloads.Count > existingExternalPoints.Count)
                    {
                        for (var index = existingExternalPoints.Count; index < updatePointPayloads.Count; index++)
                        {
                            var newPointPayload = new Dictionary<string, object?>(updatePointPayloads[index]);
                            newPointPayload.Remove("plan_sequence");
                            var newPointPayloadLog = JsonSerializer.Serialize(newPointPayload);

                            _logger.LogInformation(
                                "Creating additional external point sequentially. PlanNo={PlanNo}; TargetSequence={Sequence}; Payload={Payload}",
                                generatedPlanNo, index + 1, newPointPayloadLog);

                            var (pointCreateOk, pointCreateJson) = await _apiService.SendRequestAsync(
                                HttpMethod.Post,
                                $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(externalFleetTaskId!)}/point",
                                newPointPayload);

                            var pointCreateResponseText = pointCreateJson.ToString();
                            _logger.LogInformation(
                                "External point POST response. PlanNo={PlanNo}; TargetSequence={Sequence}; Success={Success}; Response={Response}",
                                generatedPlanNo, index + 1, pointCreateOk, pointCreateResponseText);

                            if (!pointCreateOk)
                            {
                                ModelState.AddModelError(string.Empty,
                                    $"Gagal menambahkan point eksternal untuk urutan {index + 1}. Database lokal belum disimpan. Karena respons jaringan bisa tidak memastikan apakah point sudah terbentuk, Save ulang akan membaca point eksternal terbaru sebelum meneruskan. Respons API: {pointCreateResponseText}");
                                await RepopulateFormAsync(vm);
                                return View("Form", vm);
                            }
                        }
                    }

                    // 5) Verify the result from the external API before saving locally.
                    var verifyResult = await ReadExternalPointsAsync(externalFleetTaskId!);
                    if (!verifyResult.Success)
                    {
                        ModelState.AddModelError(string.Empty,
                            "Update eksternal telah dijalankan, tetapi verifikasi point akhir gagal. Database lokal belum disimpan. Save ulang akan membaca point terbaru. " + verifyResult.Response);
                        await RepopulateFormAsync(vm);
                        return View("Form", vm);
                    }

                    var verifiedPoints = verifyResult.Points.OrderBy(point => point.Sequence).ToList();
                    var sequenceIsCorrect = verifiedPoints.Count == updatePointPayloads.Count &&
                        verifiedPoints.Select((point, index) => point.Sequence == index + 1).All(matches => matches);

                    if (!sequenceIsCorrect)
                    {
                        var actualSequences = string.Join(", ", verifiedPoints.Select(point => point.Sequence));
                        ModelState.AddModelError(string.Empty,
                            $"Verifikasi sinkronisasi point gagal. Seharusnya terdapat {updatePointPayloads.Count} point dengan urutan 1..{updatePointPayloads.Count}, tetapi API mengembalikan {verifiedPoints.Count} point dengan sequence [{actualSequences}]. Database lokal belum disimpan. Periksa data eksternal sebelum mencoba Save ulang.");
                        await RepopulateFormAsync(vm);
                        return View("Form", vm);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "External Fleet Task synchronization failed. PlanNo={PlanNo}; ExternalFleetTaskId={ExternalFleetTaskId}",
                    generatedPlanNo, externalFleetTaskId);
                ModelState.AddModelError(string.Empty,
                    "Gagal menyinkronkan data ke sistem eksternal. Database lokal belum disimpan. Jika sebagian request sudah berhasil, Save ulang akan membaca ulang point eksternal. Detail: " + ex.Message);
                await RepopulateFormAsync(vm);
                return View("Form", vm);
            }

            // External API and local database cannot share a transaction. Save locally only
            // after all required external operations have succeeded and final point verification passed.
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                RoutePlanHeader plan;
                if (vm.Id == 0)
                {
                    plan = new RoutePlanHeader
                    {
                        PlanNo = generatedPlanNo ?? await GeneratePlanNoAsync(),
                        ExternalFleetTaskId = externalFleetTaskId,
                        CreatedBy = username,
                        CreatedAt = DateTime.Now,
                        Status = RoutePlanStatuses.Draft
                    };
                    _context.RoutePlans.Add(plan);
                }
                else
                {
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
                        TempData["Error"] = "Only DRAFT route plans can be edited.";
                        return RedirectToAction(nameof(Details), new { id = plan.Id });
                    }

                    _context.RoutePlanStops.RemoveRange(plan.Stops);
                }

                plan.ExternalFleetTaskId = externalFleetTaskId;
                plan.PlanDate = vm.PlanDate.Date;
                plan.RouteId = route.ID;
                plan.RouteCode = route.ROUTE_CODE;
                plan.RouteName = route.ROUTE_NAME;
                plan.CustomerCode = string.IsNullOrWhiteSpace(vm.CustomerCode) ? null : vm.CustomerCode.Trim();
                plan.PlannedDeparture = vm.PlannedDeparture;
                plan.VehicleId = vm.VehicleId;
                plan.VehicleNo = selectedVehicle.LicensePlate?.Trim();
                plan.DriverId = vm.DriverId;
                plan.DriverName = selectedDriver.Name?.Trim();
                plan.Notes = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes.Trim();
                plan.UpdatedBy = username;
                plan.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

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
                        PlannedDeparture = stop.PLANNED_DEPARTURE,
                        StopDurationMinutes = stop.STOP_DURATION_MINUTES ?? 0,
                        Notes = stop.NOTES,
                        Status = RoutePlanStopStatuses.Pending
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = vm.Id == 0
                    ? $"Route Plan {plan.PlanNo} created and synchronized as DRAFT."
                    : $"Route Plan {plan.PlanNo} updated and synchronized. Status remains DRAFT.";

                return RedirectToAction(nameof(Details), new { id = plan.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to save Route Plan {PlanId}; ExternalFleetTaskId={ExternalFleetTaskId}",
                    vm.Id, externalFleetTaskId);
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty,
                    "External synchronization may have succeeded, but saving the local Route Plan failed. Check the application log before retrying to avoid confusion about partial synchronization.");
                await RepopulateFormAsync(vm);
                return View("Form", vm);
            }

            void LogModelStateErrors()
            {
                var validationErrors = ModelState
                    .Where(entry => entry.Value != null && entry.Value.Errors.Count > 0)
                    .SelectMany(entry => entry.Value!.Errors.Select(error => $"{entry.Key}: {error.ErrorMessage}"))
                    .ToList();
                _logger.LogWarning("Route Plan validation failed: {Errors}", string.Join(" | ", validationErrors));
            }

            async Task<(bool Success, List<(string Id, int Sequence)> Points, string Response)> ReadExternalPointsAsync(string taskId)
            {
                var endpoint =
                    $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(taskId)}/point?is_order_by_actual=false";
                var (ok, json) = await _apiService.SendRequestAsync(HttpMethod.Get, endpoint);
                var responseText = json.ToString();

                if (!ok)
                    return (false, new List<(string Id, int Sequence)>(), "GET points gagal. Respons API: " + responseText);

                if (json.ValueKind != JsonValueKind.Object ||
                    !json.TryGetProperty("data", out var data) ||
                    data.ValueKind != JsonValueKind.Object ||
                    !data.TryGetProperty("points", out var pointsElement) ||
                    pointsElement.ValueKind != JsonValueKind.Array)
                {
                    return (false, new List<(string Id, int Sequence)>(),
                        "Struktur response GET points tidak sesuai schema yang diharapkan. Response: " + responseText);
                }

                var parsedPoints = new List<(string Id, int Sequence)>();
                foreach (var point in pointsElement.EnumerateArray())
                {
                    if (point.ValueKind != JsonValueKind.Object ||
                        !point.TryGetProperty("id", out var idElement) ||
                        idElement.ValueKind != JsonValueKind.String ||
                        string.IsNullOrWhiteSpace(idElement.GetString()))
                    {
                        return (false, new List<(string Id, int Sequence)>(),
                            "GET points mengembalikan point tanpa ID. Sinkronisasi dihentikan demi keamanan. Response: " + responseText);
                    }

                    if (!point.TryGetProperty("plan_sequence", out var sequenceElement) ||
                        sequenceElement.ValueKind != JsonValueKind.Number ||
                        !sequenceElement.TryGetInt32(out var sequence) || sequence < 1)
                    {
                        return (false, new List<(string Id, int Sequence)>(),
                            "GET points mengembalikan point tanpa plan_sequence valid. Sinkronisasi dihentikan demi keamanan. Response: " + responseText);
                    }

                    var pointType = point.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String
                        ? typeElement.GetString()?.Trim()
                        : null;

                    // Do not touch START/END/unknown point types because the local Master Route
                    // currently maps only DEFAULT stops and cannot safely reconstruct these points.
                    if (!string.IsNullOrWhiteSpace(pointType) &&
                        !string.Equals(pointType, "DEFAULT", StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, new List<(string Id, int Sequence)>(),
                            $"Fleet Task memiliki point type '{pointType}' yang tidak dikelola oleh Master Route. Tidak ada perubahan eksternal yang dilakukan. Response: {responseText}");
                    }

                    parsedPoints.Add((idElement.GetString()!, sequence));
                }

                if (parsedPoints.Select(point => point.Sequence).Distinct().Count() != parsedPoints.Count)
                {
                    return (false, new List<(string Id, int Sequence)>(),
                        "GET points mengembalikan plan_sequence duplikat. Sinkronisasi dihentikan agar tidak salah memilih point. Response: " + responseText);
                }

                return (true, parsedPoints.OrderBy(point => point.Sequence).ToList(), responseText);
            }
        }


        // =========================================================
        // SAVE ROUTE PLAN
        // =========================================================
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Save(RoutePlanFormViewModel vm)
        //{
        //    var username = HttpContext.Session.GetString("username") ?? "System";

        //    if (vm.Id == 0)
        //        ModelState.Remove(nameof(vm.PlanNo));
        //    ModelState.Remove(nameof(vm.Status));

        //    // 1. Basic validation
        //    if (!vm.RouteId.HasValue || vm.RouteId.Value <= 0)
        //        ModelState.AddModelError(nameof(vm.RouteId), "Please select a Master Route.");

        //    if (vm.PlanDate == default)
        //        ModelState.AddModelError(nameof(vm.PlanDate), "Plan Date is required.");

        //    if (!ModelState.IsValid)
        //    {
        //        LogModelStateErrors();
        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    // 2. Load active Master Route
        //    var route = await _context.Routes
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync(r => r.ID == vm.RouteId!.Value && r.STATUS == "ACTIVE");

        //    if (route == null)
        //    {
        //        ModelState.AddModelError(nameof(vm.RouteId), "The selected route does not exist or is inactive.");
        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    // 3. Load current Master Route stops
        //    var masterStops = await (
        //        from rs in _context.RouteStops
        //        join g in _context.Geofences on rs.GEOFENCE_TABLE_ID equals g.Id
        //        where rs.ROUTE_ID == route.ID
        //        orderby rs.SEQUENCE_NO
        //        select new
        //        {
        //            rs.ID,
        //            rs.GEOFENCE_TABLE_ID,
        //            rs.SEQUENCE_NO,
        //            rs.STOP_TYPE,
        //            rs.PLANNED_ARRIVAL,
        //            rs.PLANNED_DEPARTURE,
        //            rs.STOP_DURATION_MINUTES,
        //            rs.NOTES,
        //            g.FenceName,
        //            g.Address,
        //            g.City,
        //            g.Lat,
        //            g.Long,
        //            g.PhoneNo
        //        })
        //        .ToListAsync();

        //    if (masterStops.Count == 0)
        //    {
        //        ModelState.AddModelError(nameof(vm.RouteId), "The selected Master Route has no valid stops.");
        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    // 4. Validate customer assignment to the selected Master Route
        //    var assignedCustomerCodes = await _context.RouteCustomerAssignments
        //        .AsNoTracking()
        //        .Where(x => x.ROUTE_ID == route.ID)
        //        .Select(x => x.CUSTOMER_CODE)
        //        .Distinct()
        //        .ToListAsync();

        //    if (!string.IsNullOrWhiteSpace(vm.CustomerCode))
        //    {
        //        vm.CustomerCode = vm.CustomerCode.Trim();
        //        var customerAssigned = assignedCustomerCodes.Any(code =>
        //            string.Equals(code, vm.CustomerCode, StringComparison.OrdinalIgnoreCase));

        //        if (!customerAssigned)
        //        {
        //            ModelState.AddModelError(nameof(vm.CustomerCode),
        //                "The selected customer is not assigned to this Master Route.");
        //            await RepopulateFormAsync(vm);
        //            return View("Form", vm);
        //        }
        //    }

        //    // 5. Validate external vehicle and driver
        //    ExternalVehicle? selectedVehicle = null;
        //    ExternalDriver? selectedDriver = null;

        //    try
        //    {
        //        if (vm.VehicleId.HasValue && vm.VehicleId.Value > 0)
        //        {
        //            var vehicles = await FetchVehiclesFromApiAsync();
        //            selectedVehicle = vehicles.FirstOrDefault(v => v.Id == vm.VehicleId.Value);
        //            if (selectedVehicle == null)
        //                ModelState.AddModelError(nameof(vm.VehicleId),
        //                    "The selected vehicle is no longer available in the external fleet system. Please select it again.");
        //        }

        //        if (vm.DriverId.HasValue && vm.DriverId.Value > 0)
        //        {
        //            var drivers = await FetchDriversFromApiAsync();
        //            selectedDriver = drivers.FirstOrDefault(d => d.Id == vm.DriverId.Value);
        //            if (selectedDriver == null)
        //                ModelState.AddModelError(nameof(vm.DriverId),
        //                    "The selected driver is no longer available in the external fleet system. Please select them again.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to validate external vehicle/driver selection.");
        //        ModelState.AddModelError(string.Empty,
        //            "Unable to validate the selected vehicle or driver against the external fleet system. Please retry.");
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    // 6. Load and validate the existing local DRAFT, if this is an edit
        //    RoutePlanHeader? existingDraftPlan = null;
        //    if (vm.Id > 0)
        //    {
        //        existingDraftPlan = await _context.RoutePlans
        //            .AsNoTracking()
        //            .FirstOrDefaultAsync(x => x.Id == vm.Id);

        //        if (existingDraftPlan == null)
        //            return NotFound();

        //        if (existingDraftPlan.Status != RoutePlanStatuses.Draft)
        //        {
        //            TempData["Error"] = "Only DRAFT route plans can be edited.";
        //            return RedirectToAction(nameof(Details), new { id = existingDraftPlan.Id });
        //        }

        //        if (string.IsNullOrWhiteSpace(existingDraftPlan.ExternalFleetTaskId))
        //        {
        //            ModelState.AddModelError(string.Empty,
        //                "External Fleet Task ID is missing. Data was not saved to avoid a local/external mismatch.");
        //            await RepopulateFormAsync(vm);
        //            return View("Form", vm);
        //        }
        //    }

        //    if (selectedVehicle == null || selectedDriver == null)
        //    {
        //        if (selectedVehicle == null)
        //            ModelState.AddModelError(nameof(vm.VehicleId), "Select a valid external vehicle.");
        //        if (selectedDriver == null)
        //            ModelState.AddModelError(nameof(vm.DriverId), "Select a valid external driver.");
        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    var generatedPlanNo = vm.Id == 0
        //        ? await GeneratePlanNoAsync()
        //        : existingDraftPlan!.PlanNo;
        //    var externalFleetTaskId = existingDraftPlan?.ExternalFleetTaskId;

        //    // Temporary defaults retained from the currently working API integration.
        //    const int debugUserGroupId = 1;
        //    const string debugExternalAddressId = "1";
        //    const double debugLatitude = -7.265122697218518;
        //    const double debugLongitude = 112.74688141009835;

        //    var generalNote = string.IsNullOrWhiteSpace(vm.Notes) ? "mendesak" : vm.Notes.Trim();

        //    // Build the target points once. The same mapping is used for initial CREATE
        //    // and for one-by-one point PATCH/POST during UPDATE.
        //    var updatePointPayloads = masterStops
        //        .Select((stop, index) =>
        //        {
        //            var locationName = string.IsNullOrWhiteSpace(stop.FenceName)
        //                ? "McEasy Surabaya"
        //                : stop.FenceName.Trim();
        //            var fullAddress = string.IsNullOrWhiteSpace(stop.Address)
        //                ? locationName
        //                : stop.Address.Trim();
        //            var lat = ParseCoordinateOrFallback(stop.Lat, debugLatitude);
        //            var lng = ParseCoordinateOrFallback(stop.Long, debugLongitude);
        //            var durationMinutes = stop.STOP_DURATION_MINUTES.HasValue && stop.STOP_DURATION_MINUTES.Value > 0
        //                ? stop.STOP_DURATION_MINUTES.Value
        //                : 15;
        //            var targetArrival = ResolveTargetArrival(
        //                vm.PlanDate,
        //                vm.PlannedDeparture,
        //                stop.PLANNED_ARRIVAL,
        //                stop.SEQUENCE_NO);
        //            var targetDeparture = targetArrival.AddMinutes(durationMinutes);
        //            var stopNote = string.IsNullOrWhiteSpace(stop.NOTES) ? generalNote : stop.NOTES.Trim();

        //            // Preserve the currently working activity label until official mapping is finalized.
        //            return new Dictionary<string, object?>
        //            {
        //                //["activity_label"] = "PICKUP",
        //                ["activity_label"] = MapExternalActivityLabel(stop.STOP_TYPE),
        //                ["address"] = new Dictionary<string, object?>
        //                {
        //                    ["full_name"] = fullAddress,
        //                    ["id"] = debugExternalAddressId,
        //                    ["lat"] = lat,
        //                    ["lng"] = lng,
        //                    ["name"] = locationName,
        //                    ["type"] = "GOOGLE_MAP"
        //                },
        //                ["note"] = stopNote,
        //                ["plan_sequence"] = index + 1,
        //                ["service_time"] = $"PT{durationMinutes}M",
        //                ["target_time_arrival"] = FormatJakartaDateTime(targetArrival),
        //                ["target_time_departure"] = FormatJakartaDateTime(targetDeparture),
        //                ["type"] = "DEFAULT"
        //            };
        //        })
        //        .ToList();

        //    // The CREATE endpoint accepts point payloads without plan_sequence.
        //    var createPointPayloads = updatePointPayloads
        //        .Select(point =>
        //        {
        //            var copy = new Dictionary<string, object?>(point);
        //            copy.Remove("plan_sequence");
        //            return copy;
        //        })
        //        .ToList();

        //    var phoneNumber = string.IsNullOrWhiteSpace(selectedDriver.PhoneNo)
        //        ? "081234567890"
        //        : selectedDriver.PhoneNo;

        //    var createFleetTaskPayload = new
        //    {
        //        custom_field = new Dictionary<string, string> { ["key"] = "value" },
        //        driver_id = selectedDriver.Id,
        //        note = $"Route Plan {generatedPlanNo} - {generalNote}",
        //        phone_number = phoneNumber,
        //        points = createPointPayloads,
        //        send_late_notification = true,
        //        user_group_id = debugUserGroupId,
        //        vehicle_id = selectedVehicle.Id
        //    };

        //    // UPDATE: the external API is currently being called one header field at a time.
        //    // Keep this list limited to the three confirmed fields requested for now.
        //    // They are sent sequentially on every Save so retries can safely reapply them.
        //    var updateHeaderFieldPayloads = new List<(string FieldName, Dictionary<string, object?> Payload)>
        //    {
        //        ("driver_id", new Dictionary<string, object?>
        //        {
        //            ["driver_id"] = selectedDriver.Id
        //        }),
        //        ("vehicle_id", new Dictionary<string, object?>
        //        {
        //            ["vehicle_id"] = selectedVehicle.Id
        //        }),
        //        ("note", new Dictionary<string, object?>
        //        {
        //            ["note"] = $"Route Plan {generatedPlanNo} - {generalNote}"
        //        })
        //    };

        //    try
        //    {
        //        if (vm.Id == 0)
        //        {
        //            // CREATE: preserve the existing working flow. Create task -> transition
        //            // to DRAFT -> only then create the local record. This is not an edit.
        //            var createPayloadLog = JsonSerializer.Serialize(createFleetTaskPayload);
        //            _logger.LogInformation(
        //                "Creating external fleet task. PlanNo={PlanNo}; Payload={Payload}",
        //                generatedPlanNo, createPayloadLog);

        //            var (createOk, createJson) = await _apiService.SendRequestAsync(
        //                HttpMethod.Post,
        //                "fleet-planning/api/web/v1/fleet-task-instant",
        //                createFleetTaskPayload);

        //            var createResponseText = createJson.ToString();
        //            _logger.LogInformation(
        //                "Create external fleet task response. PlanNo={PlanNo}; Success={Success}; Response={Response}",
        //                generatedPlanNo, createOk, createResponseText);

        //            if (!createOk)
        //            {
        //                ModelState.AddModelError(string.Empty,
        //                    "External fleet task gagal dibuat. Respons API: " + createResponseText);
        //                await RepopulateFormAsync(vm);
        //                return View("Form", vm);
        //            }

        //            externalFleetTaskId = TryReadExternalFleetTaskId(createJson);
        //            if (string.IsNullOrWhiteSpace(externalFleetTaskId))
        //            {
        //                _logger.LogError(
        //                    "External task creation returned no task ID. PlanNo={PlanNo}; Response={Response}",
        //                    generatedPlanNo, createResponseText);
        //                ModelState.AddModelError(string.Empty,
        //                    "API eksternal merespons sukses tetapi ID Fleet Task tidak ditemukan. Data lokal belum disimpan untuk mencegah duplikasi. Periksa log/response API: " + createResponseText);
        //                await RepopulateFormAsync(vm);
        //                return View("Form", vm);
        //            }

        //            var (transitionOk, transitionJson) = await _apiService.SendRequestAsync(
        //                HttpMethod.Post,
        //                $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(externalFleetTaskId)}/transition",
        //                new { status = "DRAFT" });

        //            var transitionResponseText = transitionJson.ToString();
        //            _logger.LogInformation(
        //                "Initial external DRAFT transition. ExternalFleetTaskId={ExternalFleetTaskId}; Success={Success}; Response={Response}",
        //                externalFleetTaskId, transitionOk, transitionResponseText);

        //            if (!transitionOk)
        //            {
        //                ModelState.AddModelError(string.Empty,
        //                    $"External Fleet Task sudah terbentuk dengan ID {externalFleetTaskId}, tetapi transition awal ke DRAFT gagal. Data lokal belum disimpan. Jangan submit Create ulang karena dapat membuat task duplikat. Respons: {transitionResponseText}");
        //                await RepopulateFormAsync(vm);
        //                return View("Form", vm);
        //            }
        //        }
        //        else
        //        {
        //            // UPDATE: read the current planned point IDs before changing anything.
        //            // We only know how to synchronize DEFAULT points from Master Route. If
        //            // external START/END points exist, stop safely rather than deleting or
        //            // overwriting points that do not have a local Master Route equivalent.
        //            var readResult = await ReadExternalPointsAsync(externalFleetTaskId!);
        //            if (!readResult.Success)
        //            {
        //                ModelState.AddModelError(string.Empty,
        //                    "Tidak dapat membaca/validasi point eksternal. Header dan data lokal belum diubah. " + readResult.Response);
        //                await RepopulateFormAsync(vm);
        //                return View("Form", vm);
        //            }

        //            var existingExternalPoints = readResult.Points
        //                .OrderBy(point => point.Sequence)
        //                .ToList();

        //            // 1) PATCH the confirmed header fields individually and sequentially.
        //            // No /transition call is made during Save. If one request fails, stop before
        //            // touching points/local DB. Earlier successful field PATCHes may remain in
        //            // the external system; retrying Save reapplies all three fields idempotently.
        //            var headerEndpoint =
        //                $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(externalFleetTaskId!)}";

        //            foreach (var headerField in updateHeaderFieldPayloads)
        //            {
        //                var headerPayloadLog = JsonSerializer.Serialize(headerField.Payload);
        //                _logger.LogInformation(
        //                    "Patching external Fleet Task header field. PlanNo={PlanNo}; ExternalFleetTaskId={ExternalFleetTaskId}; Field={Field}; Payload={Payload}",
        //                    generatedPlanNo, externalFleetTaskId, headerField.FieldName, headerPayloadLog);

        //                var (headerOk, headerJson) = await _apiService.SendRequestAsync(
        //                    HttpMethod.Patch,
        //                    headerEndpoint,
        //                    headerField.Payload);

        //                var headerResponseText = headerJson.ToString();
        //                _logger.LogInformation(
        //                    "External Fleet Task header field PATCH response. PlanNo={PlanNo}; ExternalFleetTaskId={ExternalFleetTaskId}; Field={Field}; Success={Success}; Response={Response}",
        //                    generatedPlanNo, externalFleetTaskId, headerField.FieldName, headerOk, headerResponseText);

        //                if (!headerOk)
        //                {
        //                    ModelState.AddModelError(string.Empty,
        //                        $"Update header field '{headerField.FieldName}' ke sistem eksternal gagal. Field sebelumnya mungkin sudah berhasil di-update, tetapi points dan database lokal belum diubah. Save ulang akan mengirim kembali driver_id, vehicle_id, dan note satu per satu. Respons API: {headerResponseText}");
        //                    await RepopulateFormAsync(vm);
        //                    return View("Form", vm);
        //                }
        //            }

        //            // 2) PATCH the points that remain, matching by planned sequence.
        //            // Since both lists are sorted by sequence, the point ID is retained while
        //            // the stop data and desired plan_sequence are updated.
        //            var commonCount = Math.Min(existingExternalPoints.Count, updatePointPayloads.Count);
        //            for (var index = 0; index < commonCount; index++)
        //            {
        //                var externalPoint = existingExternalPoints[index];
        //                var pointEndpoint =
        //                    $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(externalFleetTaskId!)}/point/{Uri.EscapeDataString(externalPoint.Id)}";
        //                var pointPayloadLog = JsonSerializer.Serialize(updatePointPayloads[index]);

        //                _logger.LogInformation(
        //                    "Patching external point. PlanNo={PlanNo}; PointId={PointId}; OldSequence={OldSequence}; NewSequence={NewSequence}; Payload={Payload}",
        //                    generatedPlanNo, externalPoint.Id, externalPoint.Sequence, index + 1, pointPayloadLog);

        //                var (pointPatchOk, pointPatchJson) = await _apiService.SendRequestAsync(
        //                    HttpMethod.Patch,
        //                    pointEndpoint,
        //                    updatePointPayloads[index]);

        //                var pointPatchResponseText = pointPatchJson.ToString();
        //                _logger.LogInformation(
        //                    "External point PATCH response. PlanNo={PlanNo}; PointId={PointId}; Success={Success}; Response={Response}",
        //                    generatedPlanNo, externalPoint.Id, pointPatchOk, pointPatchResponseText);

        //                if (!pointPatchOk)
        //                {
        //                    ModelState.AddModelError(string.Empty,
        //                        $"Sinkronisasi point gagal pada urutan {index + 1} (ID {externalPoint.Id}). Header atau point sebelumnya mungkin sudah berhasil berubah di eksternal, tetapi database lokal belum disimpan. Save ulang akan membaca daftar point terbaru lalu melanjutkan sinkronisasi. Respons API: {pointPatchResponseText}");
        //                    await RepopulateFormAsync(vm);
        //                    return View("Form", vm);
        //                }
        //            }

        //            // 3) If there are extra old points, delete highest sequence first.
        //            // This avoids deleting a point still needed by the target list and follows
        //            // the sequential order required by the API.
        //            if (existingExternalPoints.Count > updatePointPayloads.Count)
        //            {
        //                var pointsToDelete = existingExternalPoints
        //                    .Skip(updatePointPayloads.Count)
        //                    .OrderByDescending(point => point.Sequence)
        //                    .ToList();

        //                foreach (var point in pointsToDelete)
        //                {
        //                    var deleteEndpoint =
        //                        $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(externalFleetTaskId!)}/point/{Uri.EscapeDataString(point.Id)}";

        //                    _logger.LogInformation(
        //                        "Deleting excess external point. PlanNo={PlanNo}; PointId={PointId}; Sequence={Sequence}",
        //                        generatedPlanNo, point.Id, point.Sequence);

        //                    var (deleteOk, deleteJson) = await _apiService.SendRequestAsync(
        //                        HttpMethod.Delete,
        //                        deleteEndpoint);

        //                    var deleteResponseText = deleteJson.ToString();
        //                    _logger.LogInformation(
        //                        "External point DELETE response. PlanNo={PlanNo}; PointId={PointId}; Success={Success}; Response={Response}",
        //                        generatedPlanNo, point.Id, deleteOk, deleteResponseText);

        //                    if (!deleteOk)
        //                    {
        //                        ModelState.AddModelError(string.Empty,
        //                            $"Gagal menghapus point eksternal urutan {point.Sequence} (ID {point.Id}). Sebagian perubahan eksternal mungkin sudah berhasil, tetapi database lokal belum disimpan. Save ulang akan membaca ulang point terbaru. Respons API: {deleteResponseText}");
        //                        await RepopulateFormAsync(vm);
        //                        return View("Form", vm);
        //                    }
        //                }
        //            }
        //            // 4) If new points are needed, POST each one sequentially. The POST
        //            // schema supplied does not accept plan_sequence, so request order is used;
        //            // the final GET below verifies the sequence assigned by the API.
        //            else if (updatePointPayloads.Count > existingExternalPoints.Count)
        //            {
        //                for (var index = existingExternalPoints.Count; index < updatePointPayloads.Count; index++)
        //                {
        //                    var newPointPayload = new Dictionary<string, object?>(updatePointPayloads[index]);
        //                    newPointPayload.Remove("plan_sequence");
        //                    var newPointPayloadLog = JsonSerializer.Serialize(newPointPayload);

        //                    _logger.LogInformation(
        //                        "Creating additional external point sequentially. PlanNo={PlanNo}; TargetSequence={Sequence}; Payload={Payload}",
        //                        generatedPlanNo, index + 1, newPointPayloadLog);

        //                    var (pointCreateOk, pointCreateJson) = await _apiService.SendRequestAsync(
        //                        HttpMethod.Post,
        //                        $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(externalFleetTaskId!)}/point",
        //                        newPointPayload);

        //                    var pointCreateResponseText = pointCreateJson.ToString();
        //                    _logger.LogInformation(
        //                        "External point POST response. PlanNo={PlanNo}; TargetSequence={Sequence}; Success={Success}; Response={Response}",
        //                        generatedPlanNo, index + 1, pointCreateOk, pointCreateResponseText);

        //                    if (!pointCreateOk)
        //                    {
        //                        ModelState.AddModelError(string.Empty,
        //                            $"Gagal menambahkan point eksternal untuk urutan {index + 1}. Database lokal belum disimpan. Karena respons jaringan bisa tidak memastikan apakah point sudah terbentuk, Save ulang akan membaca point eksternal terbaru sebelum meneruskan. Respons API: {pointCreateResponseText}");
        //                        await RepopulateFormAsync(vm);
        //                        return View("Form", vm);
        //                    }
        //                }
        //            }

        //            // 5) Verify the result from the external API before saving locally.
        //            var verifyResult = await ReadExternalPointsAsync(externalFleetTaskId!);
        //            if (!verifyResult.Success)
        //            {
        //                ModelState.AddModelError(string.Empty,
        //                    "Update eksternal telah dijalankan, tetapi verifikasi point akhir gagal. Database lokal belum disimpan. Save ulang akan membaca point terbaru. " + verifyResult.Response);
        //                await RepopulateFormAsync(vm);
        //                return View("Form", vm);
        //            }

        //            var verifiedPoints = verifyResult.Points.OrderBy(point => point.Sequence).ToList();
        //            var sequenceIsCorrect = verifiedPoints.Count == updatePointPayloads.Count &&
        //                verifiedPoints.Select((point, index) => point.Sequence == index + 1).All(matches => matches);

        //            if (!sequenceIsCorrect)
        //            {
        //                var actualSequences = string.Join(", ", verifiedPoints.Select(point => point.Sequence));
        //                ModelState.AddModelError(string.Empty,
        //                    $"Verifikasi sinkronisasi point gagal. Seharusnya terdapat {updatePointPayloads.Count} point dengan urutan 1..{updatePointPayloads.Count}, tetapi API mengembalikan {verifiedPoints.Count} point dengan sequence [{actualSequences}]. Database lokal belum disimpan. Periksa data eksternal sebelum mencoba Save ulang.");
        //                await RepopulateFormAsync(vm);
        //                return View("Form", vm);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex,
        //            "External Fleet Task synchronization failed. PlanNo={PlanNo}; ExternalFleetTaskId={ExternalFleetTaskId}",
        //            generatedPlanNo, externalFleetTaskId);
        //        ModelState.AddModelError(string.Empty,
        //            "Gagal menyinkronkan data ke sistem eksternal. Database lokal belum disimpan. Jika sebagian request sudah berhasil, Save ulang akan membaca ulang point eksternal. Detail: " + ex.Message);
        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    // External API and local database cannot share a transaction. Save locally only
        //    // after all required external operations have succeeded and final point verification passed.
        //    await using var transaction = await _context.Database.BeginTransactionAsync();
        //    try
        //    {
        //        RoutePlanHeader plan;
        //        if (vm.Id == 0)
        //        {
        //            plan = new RoutePlanHeader
        //            {
        //                PlanNo = generatedPlanNo ?? await GeneratePlanNoAsync(),
        //                ExternalFleetTaskId = externalFleetTaskId,
        //                CreatedBy = username,
        //                CreatedAt = DateTime.Now,
        //                Status = RoutePlanStatuses.Draft
        //            };
        //            _context.RoutePlans.Add(plan);
        //        }
        //        else
        //        {
        //            plan = await _context.RoutePlans
        //                .Include(x => x.Stops)
        //                .FirstOrDefaultAsync(x => x.Id == vm.Id);

        //            if (plan == null)
        //            {
        //                await transaction.RollbackAsync();
        //                return NotFound();
        //            }

        //            if (plan.Status != RoutePlanStatuses.Draft)
        //            {
        //                await transaction.RollbackAsync();
        //                TempData["Error"] = "Only DRAFT route plans can be edited.";
        //                return RedirectToAction(nameof(Details), new { id = plan.Id });
        //            }

        //            _context.RoutePlanStops.RemoveRange(plan.Stops);
        //        }

        //        plan.ExternalFleetTaskId = externalFleetTaskId;
        //        plan.PlanDate = vm.PlanDate.Date;
        //        plan.RouteId = route.ID;
        //        plan.RouteCode = route.ROUTE_CODE;
        //        plan.RouteName = route.ROUTE_NAME;
        //        plan.CustomerCode = string.IsNullOrWhiteSpace(vm.CustomerCode) ? null : vm.CustomerCode.Trim();
        //        plan.PlannedDeparture = vm.PlannedDeparture;
        //        plan.VehicleId = vm.VehicleId;
        //        plan.VehicleNo = selectedVehicle.LicensePlate?.Trim();
        //        plan.DriverId = vm.DriverId;
        //        plan.DriverName = selectedDriver.Name?.Trim();
        //        plan.Notes = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes.Trim();
        //        plan.UpdatedBy = username;
        //        plan.UpdatedAt = DateTime.Now;

        //        await _context.SaveChangesAsync();

        //        foreach (var stop in masterStops)
        //        {
        //            _context.RoutePlanStops.Add(new RoutePlanStop
        //            {
        //                RoutePlanId = plan.Id,
        //                MasterRouteStopId = stop.ID,
        //                GeofenceTableId = stop.GEOFENCE_TABLE_ID,
        //                SequenceNo = stop.SEQUENCE_NO,
        //                StopType = stop.STOP_TYPE,
        //                FenceName = stop.FenceName ?? "(Unnamed Location)",
        //                Address = stop.Address,
        //                City = stop.City,
        //                PlannedArrival = stop.PLANNED_ARRIVAL,
        //                PlannedDeparture = stop.PLANNED_DEPARTURE,
        //                StopDurationMinutes = stop.STOP_DURATION_MINUTES ?? 0,
        //                Notes = stop.NOTES,
        //                Status = RoutePlanStopStatuses.Pending
        //            });
        //        }

        //        await _context.SaveChangesAsync();
        //        await transaction.CommitAsync();

        //        TempData["Success"] = vm.Id == 0
        //            ? $"Route Plan {plan.PlanNo} created and synchronized as DRAFT."
        //            : $"Route Plan {plan.PlanNo} updated and synchronized. Status remains DRAFT.";

        //        return RedirectToAction(nameof(Details), new { id = plan.Id });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex,
        //            "Failed to save Route Plan {PlanId}; ExternalFleetTaskId={ExternalFleetTaskId}",
        //            vm.Id, externalFleetTaskId);
        //        await transaction.RollbackAsync();
        //        ModelState.AddModelError(string.Empty,
        //            "External synchronization may have succeeded, but saving the local Route Plan failed. Check the application log before retrying to avoid confusion about partial synchronization.");
        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    void LogModelStateErrors()
        //    {
        //        var validationErrors = ModelState
        //            .Where(entry => entry.Value != null && entry.Value.Errors.Count > 0)
        //            .SelectMany(entry => entry.Value!.Errors.Select(error => $"{entry.Key}: {error.ErrorMessage}"))
        //            .ToList();
        //        _logger.LogWarning("Route Plan validation failed: {Errors}", string.Join(" | ", validationErrors));
        //    }

        //    async Task<(bool Success, List<(string Id, int Sequence)> Points, string Response)> ReadExternalPointsAsync(string taskId)
        //    {
        //        var endpoint =
        //            $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(taskId)}/point?is_order_by_actual=false";
        //        var (ok, json) = await _apiService.SendRequestAsync(HttpMethod.Get, endpoint);
        //        var responseText = json.ToString();

        //        if (!ok)
        //            return (false, new List<(string Id, int Sequence)>(), "GET points gagal. Respons API: " + responseText);

        //        if (json.ValueKind != JsonValueKind.Object ||
        //            !json.TryGetProperty("data", out var data) ||
        //            data.ValueKind != JsonValueKind.Object ||
        //            !data.TryGetProperty("points", out var pointsElement) ||
        //            pointsElement.ValueKind != JsonValueKind.Array)
        //        {
        //            return (false, new List<(string Id, int Sequence)>(),
        //                "Struktur response GET points tidak sesuai schema yang diharapkan. Response: " + responseText);
        //        }

        //        var parsedPoints = new List<(string Id, int Sequence)>();
        //        foreach (var point in pointsElement.EnumerateArray())
        //        {
        //            if (point.ValueKind != JsonValueKind.Object ||
        //                !point.TryGetProperty("id", out var idElement) ||
        //                idElement.ValueKind != JsonValueKind.String ||
        //                string.IsNullOrWhiteSpace(idElement.GetString()))
        //            {
        //                return (false, new List<(string Id, int Sequence)>(),
        //                    "GET points mengembalikan point tanpa ID. Sinkronisasi dihentikan demi keamanan. Response: " + responseText);
        //            }

        //            if (!point.TryGetProperty("plan_sequence", out var sequenceElement) ||
        //                sequenceElement.ValueKind != JsonValueKind.Number ||
        //                !sequenceElement.TryGetInt32(out var sequence) || sequence < 1)
        //            {
        //                return (false, new List<(string Id, int Sequence)>(),
        //                    "GET points mengembalikan point tanpa plan_sequence valid. Sinkronisasi dihentikan demi keamanan. Response: " + responseText);
        //            }

        //            var pointType = point.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String
        //                ? typeElement.GetString()?.Trim()
        //                : null;

        //            // Do not touch START/END/unknown point types because the local Master Route
        //            // currently maps only DEFAULT stops and cannot safely reconstruct these points.
        //            if (!string.IsNullOrWhiteSpace(pointType) &&
        //                !string.Equals(pointType, "DEFAULT", StringComparison.OrdinalIgnoreCase))
        //            {
        //                return (false, new List<(string Id, int Sequence)>(),
        //                    $"Fleet Task memiliki point type '{pointType}' yang tidak dikelola oleh Master Route. Tidak ada perubahan eksternal yang dilakukan. Response: {responseText}");
        //            }

        //            parsedPoints.Add((idElement.GetString()!, sequence));
        //        }

        //        if (parsedPoints.Select(point => point.Sequence).Distinct().Count() != parsedPoints.Count)
        //        {
        //            return (false, new List<(string Id, int Sequence)>(),
        //                "GET points mengembalikan plan_sequence duplikat. Sinkronisasi dihentikan agar tidak salah memilih point. Response: " + responseText);
        //        }

        //        return (true, parsedPoints.OrderBy(point => point.Sequence).ToList(), responseText);
        //    }
        //}

        //public async Task<IActionResult> Save(RoutePlanFormViewModel vm)
        //{
        //    var username =
        //        HttpContext.Session.GetString("username") ?? "System";

        //    // PlanNo is generated automatically by the server.
        //    if (vm.Id == 0)
        //    {
        //        ModelState.Remove(nameof(vm.PlanNo));
        //    }

        //    // Status is controlled by the server.
        //    ModelState.Remove(nameof(vm.Status));

        //    // -----------------------------------------------------
        //    // 1. Basic validation
        //    // -----------------------------------------------------
        //    if (!vm.RouteId.HasValue || vm.RouteId.Value <= 0)
        //    {
        //        ModelState.AddModelError(
        //            nameof(vm.RouteId),
        //            "Please select a Master Route.");
        //    }

        //    if (vm.PlanDate == default)
        //    {
        //        ModelState.AddModelError(
        //            nameof(vm.PlanDate),
        //            "Plan Date is required.");
        //    }

        //    //if (!ModelState.IsValid)
        //    //{
        //    //    await RepopulateFormAsync(vm);
        //    //    return View("Form", vm);
        //    //}

        //    if (!ModelState.IsValid)
        //    {
        //        var validationErrors = ModelState
        //            .Where(x => x.Value != null && x.Value.Errors.Count > 0)
        //            .SelectMany(x => x.Value!.Errors.Select(e =>
        //                $"{x.Key}: {e.ErrorMessage}"))
        //            .ToList();

        //        _logger.LogWarning(
        //            "Route Plan validation failed: {Errors}",
        //            string.Join(" | ", validationErrors));

        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    // -----------------------------------------------------
        //    // 2. Load ACTIVE Master Route
        //    //
        //    // IMPORTANT:
        //    // Master Route properties use uppercase names:
        //    // ID, ROUTE_CODE, ROUTE_NAME, STATUS.
        //    // -----------------------------------------------------
        //    var route = await _context.Routes
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync(r =>
        //            r.ID == vm.RouteId!.Value &&
        //            r.STATUS == "ACTIVE");

        //    if (route == null)
        //    {
        //        ModelState.AddModelError(
        //            nameof(vm.RouteId),
        //            "The selected route does not exist or is inactive.");

        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    // -----------------------------------------------------
        //    // 3. Load stops from the current Master Route
        //    //
        //    // Master Route stop properties:
        //    // ROUTE_ID, GEOFENCE_TABLE_ID, SEQUENCE_NO,
        //    // STOP_TYPE, PLANNED_ARRIVAL,
        //    // STOP_DURATION_MINUTES, NOTES.
        //    // -----------------------------------------------------
        //    var masterStops = await (
        //        from rs in _context.RouteStops
        //        join g in _context.Geofences
        //            on rs.GEOFENCE_TABLE_ID equals g.Id
        //        where rs.ROUTE_ID == route.ID
        //        orderby rs.SEQUENCE_NO
        //        select new
        //        {
        //            rs.ID,
        //            rs.GEOFENCE_TABLE_ID,
        //            rs.SEQUENCE_NO,
        //            rs.STOP_TYPE,
        //            rs.PLANNED_ARRIVAL,
        //            rs.STOP_DURATION_MINUTES,
        //            rs.NOTES,
        //            g.FenceName,
        //            g.Address,
        //            g.City,
        //            g.Lat,
        //            g.Long,
        //            g.PhoneNo
        //        })
        //        .ToListAsync();

        //    if (masterStops.Count == 0)
        //    {
        //        ModelState.AddModelError(
        //            nameof(vm.RouteId),
        //            "The selected Master Route has no valid stops.");

        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    // -----------------------------------------------------
        //    // 4. Validate customer assignment
        //    //
        //    // RouteCustomerAssignment.CUSTOMER_CODE stores
        //    // CustomerGroup.CUST_CODE.
        //    // -----------------------------------------------------
        //    var assignedCustomerCodes = await _context
        //        .RouteCustomerAssignments
        //        .AsNoTracking()
        //        .Where(x => x.ROUTE_ID == route.ID)
        //        .Select(x => x.CUSTOMER_CODE)
        //        .Distinct()
        //        .ToListAsync();

        //    if (!string.IsNullOrWhiteSpace(vm.CustomerCode))
        //    {
        //        vm.CustomerCode = vm.CustomerCode.Trim();

        //        var customerAssigned = assignedCustomerCodes.Any(
        //            code => string.Equals(
        //                code,
        //                vm.CustomerCode,
        //                StringComparison.OrdinalIgnoreCase));

        //        if (!customerAssigned)
        //        {
        //            ModelState.AddModelError(
        //                nameof(vm.CustomerCode),
        //                "The selected customer is not assigned to this Master Route.");

        //            await RepopulateFormAsync(vm);
        //            return View("Form", vm);
        //        }
        //    }

        //    // -----------------------------------------------------
        //    // 5. Validate selected external vehicle and driver IDs
        //    // -----------------------------------------------------
        //    ExternalVehicle? selectedVehicle = null;
        //    ExternalDriver? selectedDriver = null;

        //    try
        //    {
        //        if (vm.VehicleId.HasValue && vm.VehicleId.Value > 0)
        //        {
        //            var apiVehicles = await FetchVehiclesFromApiAsync();
        //            selectedVehicle = apiVehicles.FirstOrDefault(v => v.Id == vm.VehicleId.Value);

        //            if (selectedVehicle == null)
        //            {
        //                ModelState.AddModelError(nameof(vm.VehicleId),
        //                    "The selected vehicle is no longer available in the external fleet system. Please select it again.");
        //            }
        //        }

        //        if (vm.DriverId.HasValue && vm.DriverId.Value > 0)
        //        {
        //            var apiDrivers = await FetchDriversFromApiAsync();
        //            selectedDriver = apiDrivers.FirstOrDefault(d => d.Id == vm.DriverId.Value);

        //            if (selectedDriver == null)
        //            {
        //                ModelState.AddModelError(nameof(vm.DriverId),
        //                    "The selected driver is no longer available in the external fleet system. Please select them again.");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to validate external vehicle/driver selection.");
        //        ModelState.AddModelError(string.Empty,
        //            "Unable to validate the selected vehicle or driver against the external fleet system. Please retry.");
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    // -----------------------------------------------------
        //    // 6. PREPARE EXTERNAL SYNC
        //    // First Save: create the external task and transition it to DRAFT.
        //    // Subsequent Save: PATCH the existing external task without changing status.
        //    // Status changes are handled only by the dedicated actions on Details.
        //    // -----------------------------------------------------
        //    RoutePlanHeader? existingDraftPlan = null;
        //    if (vm.Id > 0)
        //    {
        //        existingDraftPlan = await _context.RoutePlans
        //            .AsNoTracking()
        //            .FirstOrDefaultAsync(x => x.Id == vm.Id);

        //        if (existingDraftPlan == null)
        //            return NotFound();

        //        if (existingDraftPlan.Status != RoutePlanStatuses.Draft)
        //        {
        //            TempData["Error"] = "Only DRAFT route plans can be edited.";
        //            return RedirectToAction(nameof(Details), new { id = existingDraftPlan.Id });
        //        }

        //        if (string.IsNullOrWhiteSpace(existingDraftPlan.ExternalFleetTaskId))
        //        {
        //            ModelState.AddModelError(string.Empty,
        //                "External Fleet Task ID is missing. Data was not saved to avoid local/external data mismatch.");
        //            await RepopulateFormAsync(vm);
        //            return View("Form", vm);
        //        }
        //    }

        //    if (selectedVehicle == null || selectedDriver == null)
        //    {
        //        if (selectedVehicle == null)
        //            ModelState.AddModelError(nameof(vm.VehicleId), "Select a valid external vehicle.");
        //        if (selectedDriver == null)
        //            ModelState.AddModelError(nameof(vm.DriverId), "Select a valid external driver.");

        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    string? generatedPlanNo = vm.Id == 0
        //        ? await GeneratePlanNoAsync()
        //        : existingDraftPlan!.PlanNo;
        //    string? externalFleetTaskId = existingDraftPlan?.ExternalFleetTaskId;

        //    // Temporary defaults retained from the working API integration.
        //    // Replace these mappings with the official master-data mapping when available.
        //    const int debugUserGroupId = 1;
        //    const string debugExternalAddressId = "1";
        //    const double debugLatitude = -7.265122697218518;
        //    const double debugLongitude = 112.74688141009835;

        //    var generalNote = string.IsNullOrWhiteSpace(vm.Notes)
        //        ? "mendesak"
        //        : vm.Notes.Trim();

        //    var points = masterStops.Select(stop =>
        //    {
        //        var locationName = string.IsNullOrWhiteSpace(stop.FenceName)
        //            ? "McEasy Surabaya"
        //            : stop.FenceName.Trim();
        //        var fullAddress = string.IsNullOrWhiteSpace(stop.Address)
        //            ? locationName
        //            : stop.Address.Trim();
        //        var lat = ParseCoordinateOrFallback(stop.Lat, debugLatitude);
        //        var lng = ParseCoordinateOrFallback(stop.Long, debugLongitude);
        //        var durationMinutes = stop.STOP_DURATION_MINUTES.HasValue && stop.STOP_DURATION_MINUTES.Value > 0
        //            ? stop.STOP_DURATION_MINUTES.Value
        //            : 15;
        //        var targetArrival = ResolveTargetArrival(
        //            vm.PlanDate,
        //            vm.PlannedDeparture,
        //            stop.PLANNED_ARRIVAL,
        //            stop.SEQUENCE_NO);
        //        var targetDeparture = targetArrival.AddMinutes(durationMinutes);
        //        var stopNote = string.IsNullOrWhiteSpace(stop.NOTES)
        //            ? generalNote
        //            : stop.NOTES.Trim();

        //        // Keep the current known-good API value until the official activity mapping is finalized.
        //        const string activityLabel = "PICKUP";

        //        return new
        //        {
        //            activity_label = activityLabel,
        //            address = new
        //            {
        //                full_name = fullAddress,
        //                id = debugExternalAddressId,
        //                lat,
        //                lng,
        //                name = locationName,
        //                type = "GOOGLE_MAP"
        //            },
        //            note = stopNote,
        //            service_time = $"PT{durationMinutes}M",
        //            target_time_arrival = FormatJakartaDateTime(targetArrival),
        //            target_time_departure = FormatJakartaDateTime(targetDeparture),
        //            type = "DEFAULT"
        //        };
        //    }).ToList();

        //    var fleetTaskPayload = new
        //    {
        //        custom_field = new Dictionary<string, string>
        //        {
        //            ["key"] = "value"
        //        },
        //        driver_id = selectedDriver.Id,
        //        note = $"Route Plan {generatedPlanNo} - {generalNote}",
        //        phone_number = string.IsNullOrWhiteSpace(selectedDriver.PhoneNo)
        //            ? "081234567890"
        //            : selectedDriver.PhoneNo,
        //        points,
        //        send_late_notification = true,
        //        user_group_id = debugUserGroupId,
        //        vehicle_id = selectedVehicle.Id
        //    };

        //    var fleetTaskPayloadUpdate = new
        //    {
        //        auto_start_type = "GEOFENCE",
        //        custom_field = new Dictionary<string, string>
        //        {
        //            ["key"] = "value"
        //        },
        //        driver_id = selectedDriver.Id,
        //        note = $"Route Plan {generatedPlanNo} - {generalNote}",
        //        phone_number = string.IsNullOrWhiteSpace(selectedDriver.PhoneNo)
        //        ? "081234567890"
        //        : selectedDriver.PhoneNo,
        //        //points,
        //        send_late_notification = true,
        //        user_group_id = debugUserGroupId,
        //        vehicle_id = selectedVehicle.Id
        //    };

        //    try
        //    {
        //        var payloadLog = JsonSerializer.Serialize(fleetTaskPayload);

        //        if (vm.Id == 0)
        //        {
        //            _logger.LogInformation(
        //                "Creating external fleet task. PlanNo={PlanNo}; Payload={Payload}",
        //                generatedPlanNo, payloadLog);

        //            var (externalOk, externalJson) = await _apiService.SendRequestAsync(
        //                HttpMethod.Post,
        //                "fleet-planning/api/web/v1/fleet-task-instant",
        //                fleetTaskPayload);

        //            var externalResponseText = externalJson.ToString();
        //            _logger.LogInformation(
        //                "Create external fleet task response. PlanNo={PlanNo}; Success={Success}; Response={Response}",
        //                generatedPlanNo, externalOk, externalResponseText);

        //            if (!externalOk)
        //            {
        //                ModelState.AddModelError(string.Empty,
        //                    "External fleet task gagal dibuat. Respons API: " + externalResponseText);
        //                await RepopulateFormAsync(vm);
        //                return View("Form", vm);
        //            }

        //            externalFleetTaskId = TryReadExternalFleetTaskId(externalJson);
        //            if (string.IsNullOrWhiteSpace(externalFleetTaskId))
        //            {
        //                _logger.LogError(
        //                    "External task creation returned no task ID. PlanNo={PlanNo}; Response={Response}",
        //                    generatedPlanNo, externalResponseText);
        //                ModelState.AddModelError(string.Empty,
        //                    "API eksternal merespons sukses tetapi ID Fleet Task tidak ditemukan. Data lokal belum disimpan untuk mencegah duplikasi. Periksa log dan respons API: " + externalResponseText);
        //                await RepopulateFormAsync(vm);
        //                return View("Form", vm);
        //            }

        //            // Only the first Save initializes the external status to DRAFT.
        //            var transitionPayload = new { status = "DRAFT" };
        //            var (transitionOk, transitionJson) = await _apiService.SendRequestAsync(
        //                HttpMethod.Post,
        //                $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(externalFleetTaskId)}/transition",
        //                transitionPayload);

        //            var transitionResponseText = transitionJson.ToString();
        //            _logger.LogInformation(
        //                "Initial external DRAFT transition. ExternalFleetTaskId={ExternalFleetTaskId}; Success={Success}; Response={Response}",
        //                externalFleetTaskId, transitionOk, transitionResponseText);

        //            if (!transitionOk)
        //            {
        //                ModelState.AddModelError(string.Empty,
        //                    $"External Fleet Task sudah terbentuk dengan ID {externalFleetTaskId}, tetapi transition awal ke DRAFT gagal. Data lokal belum disimpan. Jangan submit Create ulang karena dapat membuat task duplikat. Respons: {transitionResponseText}");
        //                await RepopulateFormAsync(vm);
        //                return View("Form", vm);
        //            }
        //        }
        //        else
        //        {
        //            // Editing a DRAFT updates the same external task. Do not call /transition here.
        //            var updateEndpoint =
        //                $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(externalFleetTaskId!)}";

        //            _logger.LogInformation(
        //                "Updating existing external DRAFT task. PlanNo={PlanNo}; ExternalFleetTaskId={ExternalFleetTaskId}; Payload={Payload}",
        //                generatedPlanNo, externalFleetTaskId, payloadLog);

        //            var (updateOk, updateJson) = await _apiService.SendRequestAsync(
        //                HttpMethod.Patch,
        //                updateEndpoint,
        //                fleetTaskPayloadUpdate);

        //            var updateResponseText = updateJson.ToString();
        //            _logger.LogInformation(
        //                "Update external fleet task response. PlanNo={PlanNo}; ExternalFleetTaskId={ExternalFleetTaskId}; Success={Success}; Response={Response}",
        //                generatedPlanNo, externalFleetTaskId, updateOk, updateResponseText);

        //            if (!updateOk)
        //            {
        //                ModelState.AddModelError(string.Empty,
        //                    "Update ke sistem eksternal gagal. Data lokal tidak diubah agar tetap sinkron. Respons API: " + updateResponseText);
        //                await RepopulateFormAsync(vm);
        //                return View("Form", vm);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex,
        //            "External Fleet Task sync failed. PlanNo={PlanNo}; ExternalFleetTaskId={ExternalFleetTaskId}",
        //            generatedPlanNo, externalFleetTaskId);
        //        ModelState.AddModelError(string.Empty,
        //            "Gagal menyinkronkan data ke sistem eksternal. Data lokal belum disimpan. Detail: " + ex.Message);
        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    // NOTE: API and local database cannot share one transaction. If external
        //    // creation succeeds but local save fails, the external task may remain orphaned.
        //    // The external task ID is logged and persisted to make recovery possible.

        //    // -----------------------------------------------------
        //    // 7. Begin local transaction AFTER the external create
        //    // -----------------------------------------------------
        //    await using var transaction =
        //        await _context.Database.BeginTransactionAsync();

        //    try
        //    {
        //        RoutePlanHeader plan;

        //        if (vm.Id == 0)
        //        {
        //            // CREATE
        //            plan = new RoutePlanHeader
        //            {
        //                PlanNo = generatedPlanNo ?? await GeneratePlanNoAsync(),
        //                ExternalFleetTaskId = externalFleetTaskId,
        //                CreatedBy = username,
        //                CreatedAt = DateTime.Now,
        //                Status = RoutePlanStatuses.Draft
        //            };

        //            _context.RoutePlans.Add(plan);
        //        }
        //        else
        //        {
        //            // EDIT
        //            plan = await _context.RoutePlans
        //                .Include(x => x.Stops)
        //                .FirstOrDefaultAsync(x => x.Id == vm.Id);

        //            if (plan == null)
        //            {
        //                await transaction.RollbackAsync();
        //                return NotFound();
        //            }

        //            if (plan.Status != RoutePlanStatuses.Draft)
        //            {
        //                await transaction.RollbackAsync();

        //                TempData["Error"] =
        //                    "Only DRAFT route plans can be edited.";

        //                return RedirectToAction(
        //                    nameof(Details),
        //                    new { id = plan.Id });
        //            }

        //            // Replace the stop snapshot for this DRAFT plan.
        //            _context.RoutePlanStops.RemoveRange(plan.Stops);
        //        }

        //        // -------------------------------------------------
        //        // 6. Save header
        //        // -------------------------------------------------
        //        plan.PlanDate = vm.PlanDate.Date;
        //        plan.RouteId = route.ID;
        //        plan.RouteCode = route.ROUTE_CODE;
        //        plan.RouteName = route.ROUTE_NAME;

        //        plan.CustomerCode =
        //            string.IsNullOrWhiteSpace(vm.CustomerCode)
        //                ? null
        //                : vm.CustomerCode.Trim();

        //        plan.PlannedDeparture = vm.PlannedDeparture;

        //        plan.VehicleId = vm.VehicleId;
        //        plan.VehicleNo = selectedVehicle != null
        //            ? selectedVehicle.LicensePlate?.Trim()
        //            : (string.IsNullOrWhiteSpace(vm.VehicleNo) ? null : vm.VehicleNo.Trim());

        //        plan.DriverId = vm.DriverId;
        //        plan.DriverName = selectedDriver != null
        //            ? selectedDriver.Name?.Trim()
        //            : (string.IsNullOrWhiteSpace(vm.DriverName) ? null : vm.DriverName.Trim());

        //        plan.Notes =
        //            string.IsNullOrWhiteSpace(vm.Notes)
        //                ? null
        //                : vm.Notes.Trim();

        //        plan.UpdatedBy = username;
        //        plan.UpdatedAt = DateTime.Now;

        //        await _context.SaveChangesAsync();

        //        // -------------------------------------------------
        //        // 7. Snapshot Master Route stops
        //        //
        //        // Route Plan retains the location data at the
        //        // time of saving, even if the master changes later.
        //        // -------------------------------------------------
        //        foreach (var stop in masterStops)
        //        {
        //            _context.RoutePlanStops.Add(new RoutePlanStop
        //            {
        //                RoutePlanId = plan.Id,
        //                MasterRouteStopId = stop.ID,
        //                GeofenceTableId = stop.GEOFENCE_TABLE_ID,
        //                SequenceNo = stop.SEQUENCE_NO,
        //                StopType = stop.STOP_TYPE,
        //                FenceName = stop.FenceName ?? "(Unnamed Location)",
        //                Address = stop.Address,
        //                City = stop.City,
        //                PlannedArrival = stop.PLANNED_ARRIVAL,
        //                //StopDurationMinutes = stop.STOP_DURATION_MINUTES,
        //                StopDurationMinutes = stop.STOP_DURATION_MINUTES ?? 0,
        //                Notes = stop.NOTES,
        //                Status = RoutePlanStopStatuses.Pending
        //            });
        //        }

        //        await _context.SaveChangesAsync();
        //        await transaction.CommitAsync();

        //        TempData["Success"] = vm.Id == 0
        //            ? $"Route Plan {plan.PlanNo} created and synchronized as DRAFT."
        //            : $"Route Plan {plan.PlanNo} updated and synchronized. Status remains DRAFT.";

        //        return RedirectToAction(
        //            nameof(Details),
        //            new { id = plan.Id });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(
        //            ex,
        //            "Failed to save Route Plan {PlanId}; ExternalFleetTaskId={ExternalFleetTaskId}",
        //            vm.Id,
        //            externalFleetTaskId);

        //        await transaction.RollbackAsync();

        //        ModelState.AddModelError(
        //            string.Empty,
        //            "Unable to save the Route Plan. Check the application log for details.");

        //        await RepopulateFormAsync(vm);

        //        return View("Form", vm);
        //    }
        //}


        //public async Task<IActionResult> Save(RoutePlanFormViewModel vm)
        //{
        //    var username =
        //        HttpContext.Session.GetString("username") ?? "System";

        //    // PlanNo is generated automatically by the server.
        //    if (vm.Id == 0)
        //    {
        //        ModelState.Remove(nameof(vm.PlanNo));
        //    }

        //    // Status is controlled by the server.
        //    ModelState.Remove(nameof(vm.Status));

        //    // -----------------------------------------------------
        //    // 1. Basic validation
        //    // -----------------------------------------------------
        //    if (!vm.RouteId.HasValue || vm.RouteId.Value <= 0)
        //    {
        //        ModelState.AddModelError(
        //            nameof(vm.RouteId),
        //            "Please select a Master Route.");
        //    }

        //    if (vm.PlanDate == default)
        //    {
        //        ModelState.AddModelError(
        //            nameof(vm.PlanDate),
        //            "Plan Date is required.");
        //    }

        //    //if (!ModelState.IsValid)
        //    //{
        //    //    await RepopulateFormAsync(vm);
        //    //    return View("Form", vm);
        //    //}

        //    if (!ModelState.IsValid)
        //    {
        //        var validationErrors = ModelState
        //            .Where(x => x.Value != null && x.Value.Errors.Count > 0)
        //            .SelectMany(x => x.Value!.Errors.Select(e =>
        //                $"{x.Key}: {e.ErrorMessage}"))
        //            .ToList();

        //        _logger.LogWarning(
        //            "Route Plan validation failed: {Errors}",
        //            string.Join(" | ", validationErrors));

        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    // -----------------------------------------------------
        //    // 2. Load ACTIVE Master Route
        //    //
        //    // IMPORTANT:
        //    // Master Route properties use uppercase names:
        //    // ID, ROUTE_CODE, ROUTE_NAME, STATUS.
        //    // -----------------------------------------------------
        //    var route = await _context.Routes
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync(r =>
        //            r.ID == vm.RouteId!.Value &&
        //            r.STATUS == "ACTIVE");

        //    if (route == null)
        //    {
        //        ModelState.AddModelError(
        //            nameof(vm.RouteId),
        //            "The selected route does not exist or is inactive.");

        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    // -----------------------------------------------------
        //    // 3. Load stops from the current Master Route
        //    //
        //    // Master Route stop properties:
        //    // ROUTE_ID, GEOFENCE_TABLE_ID, SEQUENCE_NO,
        //    // STOP_TYPE, PLANNED_ARRIVAL,
        //    // STOP_DURATION_MINUTES, NOTES.
        //    // -----------------------------------------------------
        //    var masterStops = await (
        //        from rs in _context.RouteStops
        //        join g in _context.Geofences
        //            on rs.GEOFENCE_TABLE_ID equals g.Id
        //        where rs.ROUTE_ID == route.ID
        //        orderby rs.SEQUENCE_NO
        //        select new
        //        {
        //            rs.ID,
        //            rs.GEOFENCE_TABLE_ID,
        //            rs.SEQUENCE_NO,
        //            rs.STOP_TYPE,
        //            rs.PLANNED_ARRIVAL,
        //            rs.STOP_DURATION_MINUTES,
        //            rs.NOTES,
        //            g.FenceName,
        //            g.Address,
        //            g.City,
        //            g.Lat,
        //            g.Long,
        //            g.PhoneNo
        //        })
        //        .ToListAsync();

        //    if (masterStops.Count == 0)
        //    {
        //        ModelState.AddModelError(
        //            nameof(vm.RouteId),
        //            "The selected Master Route has no valid stops.");

        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    // -----------------------------------------------------
        //    // 4. Validate customer assignment
        //    //
        //    // RouteCustomerAssignment.CUSTOMER_CODE stores
        //    // CustomerGroup.CUST_CODE.
        //    // -----------------------------------------------------
        //    var assignedCustomerCodes = await _context
        //        .RouteCustomerAssignments
        //        .AsNoTracking()
        //        .Where(x => x.ROUTE_ID == route.ID)
        //        .Select(x => x.CUSTOMER_CODE)
        //        .Distinct()
        //        .ToListAsync();

        //    if (!string.IsNullOrWhiteSpace(vm.CustomerCode))
        //    {
        //        vm.CustomerCode = vm.CustomerCode.Trim();

        //        var customerAssigned = assignedCustomerCodes.Any(
        //            code => string.Equals(
        //                code,
        //                vm.CustomerCode,
        //                StringComparison.OrdinalIgnoreCase));

        //        if (!customerAssigned)
        //        {
        //            ModelState.AddModelError(
        //                nameof(vm.CustomerCode),
        //                "The selected customer is not assigned to this Master Route.");

        //            await RepopulateFormAsync(vm);
        //            return View("Form", vm);
        //        }
        //    }

        //    // -----------------------------------------------------
        //    // 5. Validate selected external vehicle and driver IDs
        //    // -----------------------------------------------------
        //    ExternalVehicle? selectedVehicle = null;
        //    ExternalDriver? selectedDriver = null;

        //    try
        //    {
        //        if (vm.VehicleId.HasValue && vm.VehicleId.Value > 0)
        //        {
        //            var apiVehicles = await FetchVehiclesFromApiAsync();
        //            selectedVehicle = apiVehicles.FirstOrDefault(v => v.Id == vm.VehicleId.Value);

        //            if (selectedVehicle == null)
        //            {
        //                ModelState.AddModelError(nameof(vm.VehicleId),
        //                    "The selected vehicle is no longer available in the external fleet system. Please select it again.");
        //            }
        //        }

        //        if (vm.DriverId.HasValue && vm.DriverId.Value > 0)
        //        {
        //            var apiDrivers = await FetchDriversFromApiAsync();
        //            selectedDriver = apiDrivers.FirstOrDefault(d => d.Id == vm.DriverId.Value);

        //            if (selectedDriver == null)
        //            {
        //                ModelState.AddModelError(nameof(vm.DriverId),
        //                    "The selected driver is no longer available in the external fleet system. Please select them again.");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to validate external vehicle/driver selection.");
        //        ModelState.AddModelError(string.Empty,
        //            "Unable to validate the selected vehicle or driver against the external fleet system. Please retry.");
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        await RepopulateFormAsync(vm);
        //        return View("Form", vm);
        //    }

        //    // -----------------------------------------------------
        //    // 6. CREATE EXTERNAL FLEET TASK FIRST (CREATE ONLY)
        //    // -----------------------------------------------------
        //    // Debug defaults below are intentionally hard-coded for the first API test.
        //    // Replace them with real mappings after the API validation response is known.
        //    string? generatedPlanNo = null;
        //    string? externalFleetTaskId = null;

        //    if (vm.Id == 0)
        //    {
        //        if (selectedVehicle == null || selectedDriver == null)
        //        {
        //            if (selectedVehicle == null)
        //                ModelState.AddModelError(nameof(vm.VehicleId), "Select a vehicle before creating an external fleet task.");
        //            if (selectedDriver == null)
        //                ModelState.AddModelError(nameof(vm.DriverId), "Select a driver before creating an external fleet task.");

        //            await RepopulateFormAsync(vm);
        //            return View("Form", vm);
        //        }

        //        generatedPlanNo = await GeneratePlanNoAsync();
        //        try
        //        {
        //            const int debugUserGroupId = 1;
        //        //            //const string debugDriverAttachmentId = "00000000-0000-0000-0000-000000000000";
        //            const string debugExternalAddressId = "1";
        //            const double debugLatitude = -7.265122697218518;
        //            const double debugLongitude = 112.74688141009835;

        //            var generalNote = string.IsNullOrWhiteSpace(vm.Notes)
        //                ? "mendesak"
        //                : vm.Notes.Trim();

        //            var points = masterStops.Select(stop =>
        //            {
        //                var locationName = string.IsNullOrWhiteSpace(stop.FenceName)
        //                    ? "McEasy Surabaya"
        //                    : stop.FenceName.Trim();
        //                var fullAddress = string.IsNullOrWhiteSpace(stop.Address)
        //                    ? locationName
        //                    : stop.Address.Trim();
        //                var lat = ParseCoordinateOrFallback(stop.Lat, debugLatitude);
        //                var lng = ParseCoordinateOrFallback(stop.Long, debugLongitude);
        //                var durationMinutes = stop.STOP_DURATION_MINUTES.HasValue && stop.STOP_DURATION_MINUTES.Value > 0
        //                    ? stop.STOP_DURATION_MINUTES.Value
        //                    : 15;
        //                var targetArrival = ResolveTargetArrival(
        //                    vm.PlanDate,
        //                    vm.PlannedDeparture,
        //                    stop.PLANNED_ARRIVAL,
        //                    stop.SEQUENCE_NO);
        //                var targetDeparture = targetArrival.AddMinutes(durationMinutes);
        //                var stopNote = string.IsNullOrWhiteSpace(stop.NOTES)
        //                    ? generalNote
        //                    : stop.NOTES.Trim();
        //                var stopType = (stop.STOP_TYPE ?? string.Empty).Trim().ToUpperInvariant();
        //                //var activityLabel = stopType.Contains("DROP") || stopType.Contains("DELIVER")
        //                //    ? "DROPOFF"
        //                //    : "PICKUP";
        //                var activityLabel = "PICKUP";

        //                return new
        //                {
        //                    activity_label = activityLabel,
        //                    address = new
        //                    {
        //                        full_name = fullAddress,
        //                        id = debugExternalAddressId,
        //                        lat,
        //                        lng,
        //                        name = locationName,
        //                        type = "GOOGLE_MAP"
        //                    },
        //                    note = stopNote,
        //                    service_time = $"PT{durationMinutes}M",
        //                    target_time_arrival = FormatJakartaDateTime(targetArrival),
        //                    target_time_departure = FormatJakartaDateTime(targetDeparture),
        //                    type = "DEFAULT"
        //                };
        //            }).ToList();

        //            var fleetTaskPayload = new
        //            {
        //                custom_field = new Dictionary<string, string>
        //                {
        //                    ["key"] = "value"
        //                },
        //                //driver_attachment_file_ids = new[] { debugDriverAttachmentId },
        //                //driver_attachment_file_ids = new[] { debugDriverAttachmentId },
        //                driver_id = selectedDriver.Id,
        //                note = $"Route Plan {generatedPlanNo} - {generalNote}",
        //                phone_number = string.IsNullOrWhiteSpace(selectedDriver.PhoneNo)
        //                    ? "081234567890"
        //                    : selectedDriver.PhoneNo,
        //                points,
        //                //sales_order_id = debugSalesOrderId,
        //                send_late_notification = true,
        //                user_group_id = debugUserGroupId,
        //                vehicle_id = selectedVehicle.Id
        //            };

        //            var payloadLog = JsonSerializer.Serialize(fleetTaskPayload);
        //            _logger.LogWarning(
        //                "DEBUG POST fleet-task-instant. PlanNo={PlanNo}; Payload={Payload}",
        //                generatedPlanNo,
        //                payloadLog);

        //            var (externalOk, externalJson) = await _apiService.SendRequestAsync(
        //                HttpMethod.Post,
        //                "fleet-planning/api/web/v1/fleet-task-instant",
        //                fleetTaskPayload);

        //            var externalResponseText = externalJson.ToString();
        //            _logger.LogWarning(
        //                "DEBUG response fleet-task-instant. PlanNo={PlanNo}; Success={Success}; Response={Response}",
        //                generatedPlanNo,
        //                externalOk,
        //                externalResponseText);

        //            if (!externalOk)
        //            {
        //                ModelState.AddModelError(
        //                    string.Empty,
        //                    "External fleet task gagal dibuat. Respons API: " + externalResponseText);
        //                await RepopulateFormAsync(vm);
        //                return View("Form", vm);
        //            }

        //            externalFleetTaskId = TryReadExternalFleetTaskId(externalJson);
        //            if (string.IsNullOrWhiteSpace(externalFleetTaskId))
        //            {
        //                _logger.LogError(
        //                    "External POST may have succeeded but no task ID could be parsed. PlanNo={PlanNo}; Response={Response}",
        //                    generatedPlanNo,
        //                    externalResponseText);
        //                ModelState.AddModelError(
        //                    string.Empty,
        //                    "API eksternal merespons sukses tetapi ID fleet task tidak ditemukan. Data lokal tidak disimpan untuk mencegah duplikasi. Periksa log dan respons API: " + externalResponseText);
        //                await RepopulateFormAsync(vm);
        //                return View("Form", vm);
        //            }

        //            _logger.LogWarning(
        //                "External fleet task created; proceeding to local DB save. PlanNo={PlanNo}; ExternalFleetTaskId={ExternalFleetTaskId}",
        //                generatedPlanNo,
        //                externalFleetTaskId);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(
        //                ex,
        //                "DEBUG POST fleet-task-instant threw an exception. PlanNo={PlanNo}",
        //                generatedPlanNo);
        //            ModelState.AddModelError(
        //                string.Empty,
        //                "Gagal menghubungi API fleet-task-instant: " + ex.Message);
        //            await RepopulateFormAsync(vm);
        //            return View("Form", vm);
        //        }
        //    }

        //    // NOTE: API and local database cannot share one transaction. If external
        //    // creation succeeds but local save fails, the external task may remain orphaned.
        //    // The external task ID is logged and persisted to make recovery possible.

        //    // -----------------------------------------------------
        //    // 7. Begin local transaction AFTER the external create
        //    // -----------------------------------------------------
        //    await using var transaction =
        //        await _context.Database.BeginTransactionAsync();

        //    try
        //    {
        //        RoutePlanHeader plan;

        //        if (vm.Id == 0)
        //        {
        //            // CREATE
        //            plan = new RoutePlanHeader
        //            {
        //                PlanNo = generatedPlanNo ?? await GeneratePlanNoAsync(),
        //                ExternalFleetTaskId = externalFleetTaskId,
        //                CreatedBy = username,
        //                CreatedAt = DateTime.Now,
        //                Status = RoutePlanStatuses.Draft
        //            };

        //            _context.RoutePlans.Add(plan);
        //        }
        //        else
        //        {
        //            // EDIT
        //            plan = await _context.RoutePlans
        //                .Include(x => x.Stops)
        //                .FirstOrDefaultAsync(x => x.Id == vm.Id);

        //            if (plan == null)
        //            {
        //                await transaction.RollbackAsync();
        //                return NotFound();
        //            }

        //            if (plan.Status != RoutePlanStatuses.Draft)
        //            {
        //                await transaction.RollbackAsync();

        //                TempData["Error"] =
        //                    "Only DRAFT route plans can be edited.";

        //                return RedirectToAction(
        //                    nameof(Details),
        //                    new { id = plan.Id });
        //            }

        //            // Replace the stop snapshot for this DRAFT plan.
        //            _context.RoutePlanStops.RemoveRange(plan.Stops);
        //        }

        //        // -------------------------------------------------
        //        // 6. Save header
        //        // -------------------------------------------------
        //        plan.PlanDate = vm.PlanDate.Date;
        //        plan.RouteId = route.ID;
        //        plan.RouteCode = route.ROUTE_CODE;
        //        plan.RouteName = route.ROUTE_NAME;

        //        plan.CustomerCode =
        //            string.IsNullOrWhiteSpace(vm.CustomerCode)
        //                ? null
        //                : vm.CustomerCode.Trim();

        //        plan.PlannedDeparture = vm.PlannedDeparture;

        //        plan.VehicleId = vm.VehicleId;
        //        plan.VehicleNo = selectedVehicle != null
        //            ? selectedVehicle.LicensePlate?.Trim()
        //            : (string.IsNullOrWhiteSpace(vm.VehicleNo) ? null : vm.VehicleNo.Trim());

        //        plan.DriverId = vm.DriverId;
        //        plan.DriverName = selectedDriver != null
        //            ? selectedDriver.Name?.Trim()
        //            : (string.IsNullOrWhiteSpace(vm.DriverName) ? null : vm.DriverName.Trim());

        //        plan.Notes =
        //            string.IsNullOrWhiteSpace(vm.Notes)
        //                ? null
        //                : vm.Notes.Trim();

        //        plan.UpdatedBy = username;
        //        plan.UpdatedAt = DateTime.Now;

        //        await _context.SaveChangesAsync();

        //        // -------------------------------------------------
        //        // 7. Snapshot Master Route stops
        //        //
        //        // Route Plan retains the location data at the
        //        // time of saving, even if the master changes later.
        //        // -------------------------------------------------
        //        foreach (var stop in masterStops)
        //        {
        //            _context.RoutePlanStops.Add(new RoutePlanStop
        //            {
        //                RoutePlanId = plan.Id,
        //                MasterRouteStopId = stop.ID,
        //                GeofenceTableId = stop.GEOFENCE_TABLE_ID,
        //                SequenceNo = stop.SEQUENCE_NO,
        //                StopType = stop.STOP_TYPE,
        //                FenceName = stop.FenceName ?? "(Unnamed Location)",
        //                Address = stop.Address,
        //                City = stop.City,
        //                PlannedArrival = stop.PLANNED_ARRIVAL,
        //                //StopDurationMinutes = stop.STOP_DURATION_MINUTES,
        //                StopDurationMinutes = stop.STOP_DURATION_MINUTES ?? 0,
        //                Notes = stop.NOTES,
        //                Status = RoutePlanStopStatuses.Pending
        //            });
        //        }

        //        await _context.SaveChangesAsync();
        //        await transaction.CommitAsync();

        //        TempData["Success"] =
        //            $"Route Plan {plan.PlanNo} saved as DRAFT.";

        //        return RedirectToAction(
        //            nameof(Details),
        //            new { id = plan.Id });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(
        //            ex,
        //            "Failed to save Route Plan {PlanId}; ExternalFleetTaskId={ExternalFleetTaskId}",
        //            vm.Id,
        //            externalFleetTaskId);

        //        await transaction.RollbackAsync();

        //        ModelState.AddModelError(
        //            string.Empty,
        //            "Unable to save the Route Plan. Check the application log for details.");

        //        await RepopulateFormAsync(vm);

        //        return View("Form", vm);
        //    }
        //}

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
                    SequenceNo = rs.SEQUENCE_NO,
                    StopType = rs.STOP_TYPE,
                    FenceName = g.FenceName ?? "(Unnamed Location)",
                    Address = g.Address,
                    City = g.City,

                    PlannedArrival = rs.PLANNED_ARRIVAL.HasValue
                        ? rs.PLANNED_ARRIVAL.Value.ToString(@"hh\:mm")
                        : null,

                    PlannedDeparture = rs.PLANNED_DEPARTURE.HasValue
                        ? rs.PLANNED_DEPARTURE.Value.ToString(@"hh\:mm")
                        : null,

                    StopDurationMinutes = rs.STOP_DURATION_MINUTES ?? 0
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
        // CONFIRM PLAN -> EXTERNAL SCHEDULED, THEN LOCAL CONFIRMED
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPlan(int id)
        {
            var plan = await _context.RoutePlans
                .Include(x => x.Stops)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (plan == null)
                return NotFound();

            if (plan.Status != RoutePlanStatuses.Draft)
                return BadRequest("Only DRAFT plans can be confirmed.");

            var errors = new List<string>();

            if (!plan.VehicleId.HasValue || plan.VehicleId.Value <= 0 ||
                string.IsNullOrWhiteSpace(plan.VehicleNo))
                errors.Add("Select a valid vehicle from the external fleet system.");

            if (!plan.DriverId.HasValue || plan.DriverId.Value <= 0 ||
                string.IsNullOrWhiteSpace(plan.DriverName))
                errors.Add("Select a valid driver from the external fleet system.");

            if (!plan.PlannedDeparture.HasValue)
                errors.Add("Planned departure is required.");

            if (plan.Stops.Count == 0)
                errors.Add("At least one stop is required.");

            if (errors.Count > 0)
            {
                TempData["Error"] = string.Join(" ", errors);
                return RedirectToAction(nameof(Details), new { id });
            }

            // Official API lifecycle: local DRAFT -> external SCHEDULED.
            // Vehicle is already assigned in the create payload.
            var transition = await TransitionExternalFleetTaskAsync(
                plan.ExternalFleetTaskId,
                "SCHEDULED");

            if (!transition.Success)
            {
                TempData["Error"] =
                    $"External fleet task gagal di-transition ke SCHEDULED. Status lokal tetap DRAFT. Respons: {transition.Response}";
                return RedirectToAction(nameof(Details), new { id });
            }

            var username = HttpContext.Session.GetString("username") ?? "System";
            var now = DateTime.Now;

            try
            {
                plan.Status = RoutePlanStatuses.Confirmed;
                plan.ConfirmedBy = username;
                plan.ConfirmedAt = now;
                plan.UpdatedBy = username;
                plan.UpdatedAt = now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Route Plan confirmed and external fleet task scheduled.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "External fleet task is SCHEDULED but local ConfirmPlan save failed. RoutePlanId={RoutePlanId}; ExternalFleetTaskId={ExternalFleetTaskId}",
                    plan.Id, plan.ExternalFleetTaskId);
                TempData["Error"] =
                    $"Task eksternal sudah SCHEDULED, tetapi penyimpanan status lokal gagal. Jangan ulangi transition tanpa mengecek status eksternal. Detail: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // =========================================================
        // START ROUTE -> EXTERNAL STARTED, THEN LOCAL IN_PROGRESS
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartRoute(int id)
        {
            var plan = await _context.RoutePlans
                .FirstOrDefaultAsync(x => x.Id == id);

            if (plan == null)
                return NotFound();

            if (plan.Status != RoutePlanStatuses.Confirmed)
                return BadRequest("Only CONFIRMED plans can be started.");

            var transition = await TransitionExternalFleetTaskAsync(
                plan.ExternalFleetTaskId,
                "STARTED");

            if (!transition.Success)
            {
                TempData["Error"] =
                    $"External fleet task gagal di-transition ke STARTED. Status lokal tetap CONFIRMED. Respons: {transition.Response}";
                return RedirectToAction(nameof(Details), new { id });
            }

            var username = HttpContext.Session.GetString("username") ?? "System";
            var now = DateTime.Now;

            try
            {
                plan.Status = RoutePlanStatuses.InProgress;
                plan.StartedBy = username;
                plan.StartedAt = now;
                plan.UpdatedBy = username;
                plan.UpdatedAt = now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Route started; external fleet task is STARTED.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "External fleet task is STARTED but local StartRoute save failed. RoutePlanId={RoutePlanId}; ExternalFleetTaskId={ExternalFleetTaskId}",
                    plan.Id, plan.ExternalFleetTaskId);
                TempData["Error"] =
                    $"Task eksternal sudah STARTED, tetapi penyimpanan status lokal gagal. Periksa status eksternal sebelum mencoba lagi. Detail: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // =========================================================
        // UPDATE STOP STATUS WITH MANUAL ACTUAL TIME
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStopStatus(
            RoutePlanStopStatusInput input,
            DateTime? actualArrivalAt,
            DateTime? actualDepartureAt,
            DateTime? completedAt)
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
                TempData["Error"] =
                    "Stop statuses can only be updated while the Route Plan is IN_PROGRESS.";
                return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
            }

            var validTransition =
                (stop.Status == RoutePlanStopStatuses.Pending &&
                 input.NewStatus == RoutePlanStopStatuses.Arrived)
                ||
                (stop.Status == RoutePlanStopStatuses.Arrived &&
                 input.NewStatus == RoutePlanStopStatuses.Completed);

            if (!validTransition)
            {
                TempData["Error"] =
                    "Invalid stop status transition. Use PENDING → ARRIVED → COMPLETED.";
                return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
            }

            var isDestination = string.Equals(
                stop.StopType?.Trim(),
                "DESTINATION",
                StringComparison.OrdinalIgnoreCase);

            // All operational times are supplied by the user through the modal.
            // DateTime.Now is used only as audit time, never as Actual Arrival/Departure.
            if (input.NewStatus == RoutePlanStopStatuses.Arrived)
            {
                if (!actualArrivalAt.HasValue)
                {
                    TempData["Error"] = "Actual Arrival date and time are required.";
                    return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
                }

                if (stop.ActualDepartureAt.HasValue &&
                    actualArrivalAt.Value > stop.ActualDepartureAt.Value)
                {
                    TempData["Error"] = "Actual Arrival cannot be later than the recorded Actual Departure.";
                    return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
                }

                stop.ActualArrivalAt = actualArrivalAt.Value;
            }
            else if (input.NewStatus == RoutePlanStopStatuses.Completed)
            {
                if (isDestination)
                {
                    // Destination has no departure time; only its completion time is recorded.
                    if (!completedAt.HasValue)
                    {
                        TempData["Error"] = "Completion date and time are required for a DESTINATION stop.";
                        return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
                    }

                    if (stop.ActualArrivalAt.HasValue && completedAt.Value < stop.ActualArrivalAt.Value)
                    {
                        TempData["Error"] = "Completion time cannot be earlier than Actual Arrival.";
                        return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
                    }

                    stop.CompletedAt = completedAt.Value;
                }
                else
                {
                    // ORIGIN / PICKUP / DELIVERY (and other non-destination types):
                    // the user-entered departure value is also the stop completion time.
                    if (!actualDepartureAt.HasValue)
                    {
                        TempData["Error"] = "Actual Departure date and time are required.";
                        return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
                    }

                    if (stop.ActualArrivalAt.HasValue && actualDepartureAt.Value < stop.ActualArrivalAt.Value)
                    {
                        TempData["Error"] = "Actual Departure cannot be earlier than Actual Arrival.";
                        return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
                    }

                    stop.ActualDepartureAt = actualDepartureAt.Value;
                    stop.CompletedAt = actualDepartureAt.Value;
                }
            }

            var username = HttpContext.Session.GetString("username") ?? "System";
            var now = DateTime.Now;
            var oldStatus = stop.Status;
            stop.Status = input.NewStatus;

            _context.RoutePlanStopHistories.Add(new RoutePlanStopHistory
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

            TempData["Success"] = input.NewStatus == RoutePlanStopStatuses.Arrived
                ? $"Stop {stop.SequenceNo} marked ARRIVED and Actual Arrival saved."
                : $"Stop {stop.SequenceNo} marked COMPLETED and its actual time saved.";

            return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
        }

        // =========================================================
        // EDIT MANUAL ACTUAL TIME WITHOUT CHANGING STOP STATUS
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStopActualTimes(
            int stopId,
            DateTime? actualArrivalAt,
            DateTime? actualDepartureAt,
            DateTime? completedAt)
        {
            var stop = await _context.RoutePlanStops
                .Include(x => x.RoutePlan)
                .FirstOrDefaultAsync(x => x.Id == stopId);

            if (stop == null || stop.RoutePlan == null)
            {
                return NotFound();
            }

            if (stop.RoutePlan.Status != RoutePlanStatuses.InProgress)
            {
                TempData["Error"] =
                    "Actual times can only be edited while the Route Plan is IN_PROGRESS.";
                return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
            }

            var isDestination = string.Equals(
                stop.StopType?.Trim(),
                "DESTINATION",
                StringComparison.OrdinalIgnoreCase);

            // Keep editing chained to the current lifecycle state:
            // ARRIVED -> edit arrival only; COMPLETED -> edit departure or destination completion only.
            if (stop.Status == RoutePlanStopStatuses.Arrived)
            {
                if (!actualArrivalAt.HasValue)
                {
                    TempData["Error"] = "Actual Arrival date and time are required for an ARRIVED stop.";
                    return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
                }

                if (stop.ActualDepartureAt.HasValue && actualArrivalAt.Value > stop.ActualDepartureAt.Value)
                {
                    TempData["Error"] = "Actual Arrival cannot be later than the recorded Actual Departure.";
                    return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
                }

                stop.ActualArrivalAt = actualArrivalAt.Value;
            }
            else if (stop.Status == RoutePlanStopStatuses.Completed && isDestination)
            {
                if (!completedAt.HasValue)
                {
                    TempData["Error"] = "Completion date and time are required for a COMPLETED DESTINATION stop.";
                    return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
                }

                if (stop.ActualArrivalAt.HasValue && completedAt.Value < stop.ActualArrivalAt.Value)
                {
                    TempData["Error"] = "Completion time cannot be earlier than Actual Arrival.";
                    return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
                }

                stop.CompletedAt = completedAt.Value;
            }
            else if (stop.Status == RoutePlanStopStatuses.Completed)
            {
                if (!actualDepartureAt.HasValue)
                {
                    TempData["Error"] = "Actual Departure date and time are required for a COMPLETED stop.";
                    return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
                }

                if (stop.ActualArrivalAt.HasValue && actualDepartureAt.Value < stop.ActualArrivalAt.Value)
                {
                    TempData["Error"] = "Actual Departure cannot be earlier than Actual Arrival.";
                    return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
                }

                stop.ActualDepartureAt = actualDepartureAt.Value;
                // Per the agreed workflow, this value is also the stop completion time.
                stop.CompletedAt = actualDepartureAt.Value;
            }
            else
            {
                TempData["Error"] =
                    "Actual Arrival can be edited only when the stop is ARRIVED; Actual Departure can be edited only when it is COMPLETED.";
                return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
            }

            var username = HttpContext.Session.GetString("username") ?? "System";
            stop.RoutePlan.UpdatedBy = username;
            stop.RoutePlan.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Actual time for stop {stop.SequenceNo} has been updated.";
            return RedirectToAction(nameof(Details), new { id = stop.RoutePlanId });
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

            // A status correction must not fabricate a manual completion timestamp.
            // Preserve the existing CompletedAt when correcting to COMPLETED; clear it
            // only when the stop is no longer in COMPLETED status.
            if (input.NewStatus != RoutePlanStopStatuses.Completed)
            {
                stop.CompletedAt = null;
            }

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
        // COMPLETE ROUTE -> EXTERNAL ENDED, THEN LOCAL COMPLETED
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRoute(int id)
        {
            var plan = await _context.RoutePlans
                .Include(x => x.Stops)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (plan == null)
                return NotFound();

            if (plan.Status != RoutePlanStatuses.InProgress)
                return BadRequest("Only IN_PROGRESS plans can be completed.");

            if (plan.Stops.Count == 0 ||
                plan.Stops.Any(s => s.Status != RoutePlanStopStatuses.Completed))
            {
                TempData["Error"] =
                    "All stops must be COMPLETED before completing the Route Plan.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Official API requires external task STARTED before ENDED.
            var transition = await TransitionExternalFleetTaskAsync(
                plan.ExternalFleetTaskId,
                "ENDED");

            if (!transition.Success)
            {
                TempData["Error"] =
                    $"External fleet task gagal di-transition ke ENDED. Status lokal tetap IN_PROGRESS. Respons: {transition.Response}";
                return RedirectToAction(nameof(Details), new { id });
            }

            var username = HttpContext.Session.GetString("username") ?? "System";
            var now = DateTime.Now;

            try
            {
                plan.Status = RoutePlanStatuses.Completed;
                plan.CompletedBy = username;
                plan.CompletedAt = now;
                plan.UpdatedBy = username;
                plan.UpdatedAt = now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Route Plan completed; external fleet task is ENDED.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "External fleet task is ENDED but local CompleteRoute save failed. RoutePlanId={RoutePlanId}; ExternalFleetTaskId={ExternalFleetTaskId}",
                    plan.Id, plan.ExternalFleetTaskId);
                TempData["Error"] =
                    $"Task eksternal sudah ENDED, tetapi penyimpanan status lokal gagal. Periksa status eksternal sebelum mencoba lagi. Detail: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // =========================================================
        // CANCEL PLAN -> EXTERNAL CANCELED, THEN LOCAL CANCELLED
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelPlan(int id, string? reason)
        {
            var plan = await _context.RoutePlans
                .FirstOrDefaultAsync(x => x.Id == id);

            if (plan == null)
                return NotFound();

            if (plan.Status != RoutePlanStatuses.Draft &&
                plan.Status != RoutePlanStatuses.Confirmed)
            {
                return BadRequest("Only DRAFT or CONFIRMED plans can be cancelled.");
            }

            // Official API requires fields.reason. The fallback keeps the current
            // UI compatible until a cancellation-reason input is added to Details.cshtml.
            var transitionReason = string.IsNullOrWhiteSpace(reason)
                ? $"Cancelled from TMSBilling Route Plan {plan.PlanNo}"
                : reason.Trim();

            var transition = await TransitionExternalFleetTaskAsync(
                plan.ExternalFleetTaskId,
                "CANCELED",
                transitionReason);

            if (!transition.Success)
            {
                TempData["Error"] =
                    $"External fleet task gagal di-transition ke CANCELED. Status lokal tidak diubah. Respons: {transition.Response}";
                return RedirectToAction(nameof(Details), new { id });
            }

            var username = HttpContext.Session.GetString("username") ?? "System";

            try
            {
                plan.Status = RoutePlanStatuses.Cancelled;
                plan.UpdatedBy = username;
                plan.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Route Plan cancelled; external fleet task is CANCELED.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "External fleet task is CANCELED but local CancelPlan save failed. RoutePlanId={RoutePlanId}; ExternalFleetTaskId={ExternalFleetTaskId}",
                    plan.Id, plan.ExternalFleetTaskId);
                TempData["Error"] =
                    $"Task eksternal sudah CANCELED, tetapi penyimpanan status lokal gagal. Periksa status eksternal sebelum mencoba lagi. Detail: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // =========================================================
        // EXTERNAL FLEET TASK TRANSITION HELPER
        // Based on the official Fleet Task Instant transition documentation.
        // =========================================================
        private async Task<(bool Success, string Response)> TransitionExternalFleetTaskAsync(
            string? externalFleetTaskId,
            string targetStatus,
            string? reason = null)
        {
            if (string.IsNullOrWhiteSpace(externalFleetTaskId))
            {
                const string message = "ExternalFleetTaskId is empty; transition was not sent.";
                _logger.LogError("{Message} TargetStatus={TargetStatus}", message, targetStatus);
                return (false, message);
            }

            object payload;
            if (string.Equals(targetStatus, "CANCELED", StringComparison.OrdinalIgnoreCase))
            {
                payload = new
                {
                    status = "CANCELED",
                    fields = new
                    {
                        reason = string.IsNullOrWhiteSpace(reason)
                            ? "Cancelled from TMSBilling"
                            : reason.Trim()
                    }
                };
            }
            else
            {
                payload = new { status = targetStatus };
            }

            var endpoint =
                $"fleet-planning/api/web/v1/fleet-task-instant/{Uri.EscapeDataString(externalFleetTaskId.Trim())}/transition";
            var payloadText = JsonSerializer.Serialize(payload);

            try
            {
                _logger.LogWarning(
                    "POST external fleet task transition. ExternalFleetTaskId={ExternalFleetTaskId}; TargetStatus={TargetStatus}; Payload={Payload}",
                    externalFleetTaskId, targetStatus, payloadText);

                var (ok, json) = await _apiService.SendRequestAsync(
                    HttpMethod.Post,
                    endpoint,
                    payload);

                var responseText = json.ToString();
                _logger.LogWarning(
                    "External fleet task transition response. ExternalFleetTaskId={ExternalFleetTaskId}; TargetStatus={TargetStatus}; Success={Success}; Response={Response}",
                    externalFleetTaskId, targetStatus, ok, responseText);

                return (ok, responseText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "External fleet task transition threw an exception. ExternalFleetTaskId={ExternalFleetTaskId}; TargetStatus={TargetStatus}",
                    externalFleetTaskId, targetStatus);
                return (false, ex.Message);
            }
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

            await PopulateVehiclesAsync(vm);
            await PopulateDriversAsync(vm);
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



        private static double ParseCoordinateOrFallback(string? coordinate, double fallback)
        {
            return double.TryParse(
                coordinate,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsed)
                ? parsed
                : fallback;
        }

        private static DateTime ResolveTargetArrival(
            DateTime planDate,
            DateTime? plannedDeparture,
            TimeSpan? plannedArrival,
            int sequenceNo)
        {
            if (plannedArrival.HasValue)
            {
                return planDate.Date.Add(plannedArrival.Value);
            }

            var anchor = plannedDeparture ?? planDate.Date.AddHours(8);
            return anchor.AddMinutes(Math.Max(0, sequenceNo - 1) * 15);
        }

        private static string FormatJakartaDateTime(DateTime value)
        {
            var localUnspecified = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
            var jakarta = new DateTimeOffset(localUnspecified, TimeSpan.FromHours(7));
            return jakarta.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
        }

        private static string? TryReadExternalFleetTaskId(JsonElement response)
        {
            if (response.ValueKind != JsonValueKind.Object ||
                !response.TryGetProperty("data", out var data))
            {
                return null;
            }

            if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("id", out var id))
            {
                if (id.ValueKind == JsonValueKind.String)
                    return id.GetString();
                if (id.ValueKind == JsonValueKind.Number)
                    return id.ToString();
            }

            // Accept a nested fleet-task shape too, in case the API wraps its result.
            if (data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("fleet_task", out var task) &&
                task.ValueKind == JsonValueKind.Object &&
                task.TryGetProperty("id", out var nestedId))
            {
                if (nestedId.ValueKind == JsonValueKind.String)
                    return nestedId.GetString();
                if (nestedId.ValueKind == JsonValueKind.Number)
                    return nestedId.ToString();
            }

            return null;
        }

        private async Task<List<ExternalDriver>> FetchDriversFromApiAsync()
        {
            // Return cached data where possible to avoid requesting every page
            // whenever the form is reopened or validation fails.
            if (_cachedDrivers != null && _cachedDriversExpiresAtUtc > DateTime.UtcNow)
            {
                return _cachedDrivers;
            }

            await _driverCacheLock.WaitAsync();
            try
            {
                if (_cachedDrivers != null && _cachedDriversExpiresAtUtc > DateTime.UtcNow)
                {
                    return _cachedDrivers;
                }

                // Request a larger page size to reduce the number of API calls.
                // The loop still follows metadata.total_page if the API caps the limit.
                const int pageSize = 100;
                const int safetyMaxPages = 1000;
                var allDrivers = new List<ExternalDriver>();
                var page = 1;
                var totalPages = 1;

                do
                {
                    var (ok, json) = await _apiService.SendRequestAsync(
                        HttpMethod.Get,
                        $"fleet-planning/api/web/v1/driver?limit={pageSize}&page={page}"
                    );

                    if (!ok)
                    {
                        throw new InvalidOperationException(
                            $"Gagal mengambil daftar driver dari API eksternal pada halaman {page}."
                        );
                    }

                    var response = json.Deserialize<ExternalDriverListResponse>();
                    if (response?.Data == null)
                    {
                        throw new InvalidOperationException(
                            "Response API driver tidak memiliki data yang valid."
                        );
                    }

                    var currentPageDrivers = response.Data.PaginatedResult
                        ?? new List<ExternalDriver>();

                    allDrivers.AddRange(currentPageDrivers);

                    totalPages = response.Metadata?.TotalPage ?? page;

                    if (currentPageDrivers.Count == 0)
                    {
                        break;
                    }

                    page++;
                }
                while (page <= totalPages && page <= safetyMaxPages);

                _cachedDrivers = allDrivers
                    .Where(d => d.Id > 0 && !string.IsNullOrWhiteSpace(d.Name))
                    .GroupBy(d => d.Id)
                    .Select(g => g.First())
                    .OrderBy(d => d.Name)
                    .ThenBy(d => d.Id)
                    .ToList();

                _cachedDriversExpiresAtUtc = DateTime.UtcNow.Add(DriverCacheDuration);
                return _cachedDrivers;
            }
            finally
            {
                _driverCacheLock.Release();
            }
        }

        private async Task PopulateDriversAsync(RoutePlanFormViewModel vm)
        {
            try
            {
                var drivers = await FetchDriversFromApiAsync();

                // DriverId is a form-only field. The database currently stores DriverName,
                // so on Edit match the saved name back to a driver where possible.
                int? selectedDriverId = null;
                if (vm.DriverId.HasValue && drivers.Any(d => d.Id == vm.DriverId.Value))
                {
                    selectedDriverId = vm.DriverId;
                }
                else if (!string.IsNullOrWhiteSpace(vm.DriverName))
                {
                    var sameNameDrivers = drivers.Where(d =>
                        string.Equals(d.Name?.Trim(), vm.DriverName.Trim(),
                            StringComparison.OrdinalIgnoreCase)).ToList();

                    // Legacy rows only contain DriverName. Auto-map only when unique;
                    // if duplicate names exist, do not silently choose the wrong driver.
                    if (sameNameDrivers.Count == 1)
                    {
                        selectedDriverId = sameNameDrivers[0].Id;
                    }
                }

                vm.DriverId = selectedDriverId;
                vm.Drivers = drivers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch external drivers.");
                vm.Drivers = new List<ExternalDriver>();

                TempData["Error"] =
                    "Daftar driver gagal dimuat dari sistem eksternal. " +
                    "Silakan muat ulang halaman.";
            }
        }

        private async Task<List<ExternalVehicle>> FetchVehiclesFromApiAsync()
        {
            var (ok, json) = await _apiService.SendRequestAsync(
                HttpMethod.Get,
                "fleet-planning/api/web/v1/nearest-vehicle?latitude=0&longitude=0"
            );

            if (!ok)
            {
                throw new InvalidOperationException(
                    "Gagal mengambil daftar kendaraan dari API eksternal."
                );
            }

            var response = json.Deserialize<NearestVehicleResponse>();

            if (response?.Data == null)
            {
                throw new InvalidOperationException(
                    "Response API kendaraan tidak memiliki data yang valid."
                );
            }

            return response.Data.Result ?? new List<ExternalVehicle>();
        }

        private async Task PopulateVehiclesAsync(RoutePlanFormViewModel vm)
        {
            try
            {
                var vehicles = await FetchVehiclesFromApiAsync();

                // Ketika Edit, pertahankan pilihan berdasarkan ID yang sudah ada.
                // Jika ID belum tersedia, cocokkan berdasarkan nomor polisi lokal.
                var selectedVehicleId = vm.VehicleId
                    ?? vehicles.FirstOrDefault(v =>
                        !string.IsNullOrWhiteSpace(vm.VehicleNo) &&
                        string.Equals(
                            v.LicensePlate?.Trim(),
                            vm.VehicleNo.Trim(),
                            StringComparison.OrdinalIgnoreCase
                        )
                    )?.Id;

                vm.VehicleId = selectedVehicleId;

                vm.Vehicles = vehicles
                    .Where(v => !string.IsNullOrWhiteSpace(v.LicensePlate))
                    .GroupBy(v => v.Id)
                    .Select(g => g.First())
                    .OrderBy(v => v.LicensePlate)
                    .Select(v => new SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = v.LicensePlate,
                        Selected = selectedVehicleId == v.Id
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch external vehicles.");

                vm.Vehicles = new List<SelectListItem>();

                TempData["Error"] =
                    "Daftar kendaraan gagal dimuat dari sistem eksternal. " +
                    "Silakan muat ulang halaman.";
            }
        }

        private static string MapExternalActivityLabel(string? stopType)
        {
            return stopType?.Trim().ToUpperInvariant() switch
            {
                "ORIGIN" => "PICKUP",
                "PICKUP" => "PICKUP",
                "DELIVERY" => "DROP",
                "DESTINATION" => "DROP",

                _ => throw new ArgumentException(
                    $"Stop Type '{stopType}' belum memiliki mapping " +
                    "ke activity_label sistem eksternal.",
                    nameof(stopType))
            };
        }
    }
}