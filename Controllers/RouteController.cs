
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class RouteController : Controller
    {
        private readonly AppDbContext _context;

        public RouteController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: Route
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var routes = await _context.Routes
                .AsNoTracking()
                .Include(r => r.Stops)
                    .ThenInclude(s => s.Geofence)
                .Include(r => r.CustomerAssignments)
                .OrderBy(r => r.ROUTE_CODE)
                .ToListAsync();

            return View(routes);
        }

        // =========================================================
        // GET: Route/Form
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Form(int? id)
        {
            RouteViewModel model;

            if (id.HasValue)
            {
                var route = await _context.Routes
                    .AsNoTracking()
                    .Include(r => r.Stops)
                    .Include(r => r.CustomerAssignments)
                    .FirstOrDefaultAsync(r => r.ID == id.Value);

                if (route == null)
                {
                    return NotFound();
                }

                model = new RouteViewModel
                {
                    ID = route.ID,
                    ROUTE_CODE = route.ROUTE_CODE,
                    ROUTE_NAME = route.ROUTE_NAME,
                    DESCRIPTION = route.DESCRIPTION,
                    ROUTE_TYPE = route.ROUTE_TYPE,
                    STATUS = route.STATUS,

                    CUSTOMER_CODES = route.CustomerAssignments
                        .Select(c => c.CUSTOMER_CODE)
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Distinct()
                        .ToList(),

                    Stops = route.Stops
                        .OrderBy(s => s.SEQUENCE_NO)
                        .Select(s => new RouteStopInput
                        {
                            GEOFENCE_TABLE_ID = s.GEOFENCE_TABLE_ID,
                            SEQUENCE_NO = s.SEQUENCE_NO,
                            STOP_TYPE = s.STOP_TYPE,
                            PLANNED_ARRIVAL = s.PLANNED_ARRIVAL,
                            PLANNED_DEPARTURE = s.PLANNED_DEPARTURE,
                            STOP_DURATION_MINUTES =
                                s.STOP_DURATION_MINUTES,
                            NOTES = s.NOTES
                        })
                        .ToList()
                };
            }
            else
            {
                model = new RouteViewModel
                {
                    ROUTE_TYPE = "MULTI_STOP",
                    STATUS = "ACTIVE",
                    CUSTOMER_CODES = new List<string>(),

                    Stops = new List<RouteStopInput>
                    {
                        new()
                        {
                            SEQUENCE_NO = 1,
                            STOP_TYPE = "ORIGIN"
                        },
                        new()
                        {
                            SEQUENCE_NO = 2,
                            STOP_TYPE = "DESTINATION"
                        }
                    }
                };
            }

            await PopulateOptionsAsync(model);

            return View(model);
        }

        // =========================================================
        // POST: Route/Save
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(RouteViewModel model)
        {
            // -----------------------------------------------------
            // 1. Normalize input
            // -----------------------------------------------------
            model.ROUTE_CODE = (model.ROUTE_CODE ?? string.Empty).Trim();
            model.ROUTE_NAME = (model.ROUTE_NAME ?? string.Empty).Trim();
            model.DESCRIPTION = model.DESCRIPTION?.Trim();

            model.ROUTE_TYPE = (model.ROUTE_TYPE ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            model.STATUS = (model.STATUS ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            model.Stops ??= new List<RouteStopInput>();

            model.CUSTOMER_CODES = (model.CUSTOMER_CODES ?? new List<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var stop in model.Stops)
            {
                stop.STOP_TYPE = (stop.STOP_TYPE ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant();

                stop.NOTES = stop.NOTES?.Trim();

                // Departure is calculated server-side as the source of truth.
                if (stop.PLANNED_ARRIVAL.HasValue &&
                    stop.STOP_DURATION_MINUTES.HasValue)
                {
                    stop.PLANNED_DEPARTURE = stop.PLANNED_ARRIVAL.Value
                        .Add(TimeSpan.FromMinutes(stop.STOP_DURATION_MINUTES.Value));
                }
                else
                {
                    stop.PLANNED_DEPARTURE = null;
                }
            }

            // -----------------------------------------------------
            // 2. Validate route fields
            // -----------------------------------------------------
            var validRouteTypes = new[]
            {
                "MULTI_STOP",
                "ROUND_TRIP"
            };

            var validStatuses = new[]
            {
                "ACTIVE",
                "INACTIVE"
            };

            var validStopTypes = new[]
            {
                "ORIGIN",
                "PICKUP",
                "DELIVERY",
                "WAYPOINT",
                "DESTINATION"
            };

            if (string.IsNullOrWhiteSpace(model.ROUTE_CODE))
            {
                ModelState.AddModelError(
                    nameof(model.ROUTE_CODE),
                    "Route Code is required.");
            }

            if (string.IsNullOrWhiteSpace(model.ROUTE_NAME))
            {
                ModelState.AddModelError(
                    nameof(model.ROUTE_NAME),
                    "Route Name is required.");
            }

            if (!validRouteTypes.Contains(model.ROUTE_TYPE))
            {
                ModelState.AddModelError(
                    nameof(model.ROUTE_TYPE),
                    "Invalid route type.");
            }

            if (!validStatuses.Contains(model.STATUS))
            {
                ModelState.AddModelError(
                    nameof(model.STATUS),
                    "Invalid status.");
            }

            // -----------------------------------------------------
            // 3. Validate stop entries
            // -----------------------------------------------------
            if (model.Stops.Count < 2)
            {
                ModelState.AddModelError(
                    nameof(model.Stops),
                    "A route must contain at least two stops.");
            }

            if (model.Stops.Any(s =>
                s.GEOFENCE_TABLE_ID <= 0 ||
                !validStopTypes.Contains(s.STOP_TYPE)))
            {
                ModelState.AddModelError(
                    nameof(model.Stops),
                    "One or more stop entries are invalid.");
            }

            if (model.Stops.Any(s =>
                s.STOP_DURATION_MINUTES.HasValue &&
                (s.STOP_DURATION_MINUTES.Value < 0 ||
                 s.STOP_DURATION_MINUTES.Value > 1440)))
            {
                ModelState.AddModelError(
                    nameof(model.Stops),
                    "Stop duration must be between 0 and 1440 minutes.");
            }

            // -----------------------------------------------------
            // 4. Validate ORIGIN and DESTINATION order
            // -----------------------------------------------------
            if (model.Stops.Count > 0)
            {
                var originIndexes = model.Stops
                    .Select((stop, index) => new { stop, index })
                    .Where(x => x.stop.STOP_TYPE == "ORIGIN")
                    .Select(x => x.index)
                    .ToList();

                var destinationIndexes = model.Stops
                    .Select((stop, index) => new { stop, index })
                    .Where(x => x.stop.STOP_TYPE == "DESTINATION")
                    .Select(x => x.index)
                    .ToList();

                if (originIndexes.Count != 1 ||
                    originIndexes[0] != 0)
                {
                    ModelState.AddModelError(
                        nameof(model.Stops),
                        "Exactly one ORIGIN must be the first stop.");
                }

                if (destinationIndexes.Count > 1 ||
                    (destinationIndexes.Count == 1 &&
                     destinationIndexes[0] != model.Stops.Count - 1))
                {
                    ModelState.AddModelError(
                        nameof(model.Stops),
                        "DESTINATION can appear only once and must be the last stop.");
                }
            }

            // -----------------------------------------------------
            // 5. Validate Route Code uniqueness
            // -----------------------------------------------------
            if (!string.IsNullOrWhiteSpace(model.ROUTE_CODE))
            {
                var routeCodeExists = await _context.Routes
                    .AnyAsync(r =>
                        r.ROUTE_CODE == model.ROUTE_CODE &&
                        r.ID != model.ID);

                if (routeCodeExists)
                {
                    ModelState.AddModelError(
                        nameof(model.ROUTE_CODE),
                        "Route Code already exists.");
                }
            }

            // -----------------------------------------------------
            // 6. Validate selected locations against TRC_GEOFENCE
            // -----------------------------------------------------
            var locationIds = model.Stops
                .Select(s => s.GEOFENCE_TABLE_ID)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var existingLocationIds = await _context.Geofences
                .AsNoTracking()
                .Where(g => locationIds.Contains(g.Id))
                .Select(g => g.Id)
                .ToListAsync();

            if (locationIds.Count != existingLocationIds.Count ||
                model.Stops.Any(s => s.GEOFENCE_TABLE_ID <= 0))
            {
                ModelState.AddModelError(
                    nameof(model.Stops),
                    "One or more selected locations do not exist.");
            }

            // -----------------------------------------------------
            // 7. Validate selected customers
            //    Customer source: existing CustomerGroups
            // -----------------------------------------------------
            var requestedCustomerCodes = model.CUSTOMER_CODES;

            var validCustomerCodes = await _context.CustomerGroups
                .AsNoTracking()
                .Where(c =>
                    c.CUST_CODE != null &&
                    requestedCustomerCodes.Contains(c.CUST_CODE))
                .Select(c => c.CUST_CODE)
                .Distinct()
                .ToListAsync();

            var validCustomerSet = new HashSet<string>(
                validCustomerCodes,
                StringComparer.OrdinalIgnoreCase);

            if (requestedCustomerCodes.Any(
                c => !validCustomerSet.Contains(c)))
            {
                ModelState.AddModelError(
                    nameof(model.CUSTOMER_CODES),
                    "One or more selected customers do not exist.");
            }

            // -----------------------------------------------------
            // 8. Return form when validation fails
            // -----------------------------------------------------
            if (!ModelState.IsValid)
            {
                await PopulateOptionsAsync(model);
                return View("Form", model);
            }

            // -----------------------------------------------------
            // 9. Save route in a database transaction
            // -----------------------------------------------------
            var username =
                HttpContext.Session.GetString("username") ?? "System";

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                RouteTable route;

                if (model.ID > 0)
                {
                    // EDIT
                    route = await _context.Routes
                        .Include(r => r.Stops)
                        .Include(r => r.CustomerAssignments)
                        .FirstOrDefaultAsync(r => r.ID == model.ID);

                    if (route == null)
                    {
                        await transaction.RollbackAsync();
                        return NotFound();
                    }

                    // Keep the existing Route Code during editing.
                    route.ROUTE_NAME = model.ROUTE_NAME;
                    route.DESCRIPTION = model.DESCRIPTION;
                    route.ROUTE_TYPE = model.ROUTE_TYPE;
                    route.STATUS = model.STATUS;
                    route.UPDATED_AT = DateTime.Now;
                    route.UPDATED_BY = username;

                    // Remove old details before inserting replacements.
                    _context.RouteStops.RemoveRange(route.Stops);

                    _context.RouteCustomerAssignments.RemoveRange(
                        route.CustomerAssignments);

                    await _context.SaveChangesAsync();
                }
                else
                {
                    // CREATE
                    route = new RouteTable
                    {
                        ROUTE_CODE = model.ROUTE_CODE,
                        ROUTE_NAME = model.ROUTE_NAME,
                        DESCRIPTION = model.DESCRIPTION,
                        ROUTE_TYPE = model.ROUTE_TYPE,
                        STATUS = model.STATUS,
                        CREATED_AT = DateTime.Now,
                        CREATED_BY = username
                    };

                    _context.Routes.Add(route);

                    // Generate the route ID first.
                    await _context.SaveChangesAsync();
                }

                // -------------------------------------------------
                // 10. Insert route stops
                // -------------------------------------------------
                for (var index = 0; index < model.Stops.Count; index++)
                {
                    var input = model.Stops[index];

                    _context.RouteStops.Add(new RouteStopTable
                    {
                        ROUTE_ID = route.ID,

                        // This references GeofenceTable.Id,
                        // the primary key of TRC_GEOFENCE.
                        GEOFENCE_TABLE_ID = input.GEOFENCE_TABLE_ID,

                        SEQUENCE_NO = index + 1,
                        STOP_TYPE = input.STOP_TYPE,
                        PLANNED_ARRIVAL = input.PLANNED_ARRIVAL,
                        PLANNED_DEPARTURE = input.PLANNED_DEPARTURE,
                        STOP_DURATION_MINUTES =
                            input.STOP_DURATION_MINUTES,
                        NOTES = input.NOTES
                    });
                }

                // -------------------------------------------------
                // 11. Insert customer assignments
                // -------------------------------------------------
                foreach (var customerCode in requestedCustomerCodes)
                {
                    _context.RouteCustomerAssignments.Add(
                        new RouteCustomerAssignment
                        {
                            ROUTE_ID = route.ID,
                            CUSTOMER_CODE = customerCode,
                            CREATED_AT = DateTime.Now
                        });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = model.ID > 0
                    ? "Route updated successfully."
                    : "Route created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to save the route. Please review the information and try again.");

                await PopulateOptionsAsync(model);

                return View("Form", model);
            }
        }

        // =========================================================
        // POST: Route/Deactivate
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var route = await _context.Routes
                .FirstOrDefaultAsync(r => r.ID == id);

            if (route == null)
            {
                return NotFound();
            }

            if (route.STATUS != "INACTIVE")
            {
                route.STATUS = "INACTIVE";
                route.UPDATED_AT = DateTime.Now;
                route.UPDATED_BY =
                    HttpContext.Session.GetString("username") ?? "System";

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Route deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateOptionsAsync(RouteViewModel model)
        {
            model.CUSTOMER_CODES ??= new List<string>();
            model.Stops ??= new List<RouteStopInput>();

            var geofences = await _context.Geofences
                .AsNoTracking()
                .Where(g => g.FenceName != null && g.FenceName.Trim() != "")
                .OrderBy(g => g.FenceName)
                .Select(g => new
                {
                    g.Id,
                    g.FenceName,
                    g.City,
                    g.Address,
                    g.Lat,
                    g.Long
                })
                .ToListAsync();

            // Dropdown location
            model.AvailableLocations = geofences
                .Select(g => new SelectListItem
                {
                    Value = g.Id.ToString(),
                    Text = g.FenceName +
                        (string.IsNullOrWhiteSpace(g.City)
                            ? string.Empty
                            : " — " + g.City)
                })
                .ToList();

            // Coordinate data for Leaflet
            ViewBag.GeofenceMapData =
                System.Text.Json.JsonSerializer.Serialize(
                    geofences.Select(g => new
                    {
                        id = g.Id,
                        name = g.FenceName,
                        city = g.City,
                        address = g.Address,
                        lat = g.Lat,
                        lng = g.Long
                    })
                );

            // Customer dropdown
            var customerCodes = await _context.CustomerGroups
                .AsNoTracking()
                .Where(c => c.CUST_CODE != null && c.CUST_CODE != "")
                .Select(c => c.CUST_CODE)
                .Distinct()
                .OrderBy(code => code)
                .ToListAsync();

            model.AvailableCustomers = customerCodes
                .Select(code => new SelectListItem
                {
                    Value = code,
                    Text = code,
                    Selected = model.CUSTOMER_CODES.Contains(
                        code,
                        StringComparer.OrdinalIgnoreCase)
                })
                .ToList();
        }

    }
}
