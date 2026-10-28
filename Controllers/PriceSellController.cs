using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Models.UploadModels;
using TMSBilling.Services;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

[SessionAuthorize]
public class PriceSellController : Controller
{
    private readonly AppDbContext _context;
    private readonly SelectListService _selectList;
    private readonly PriceSellApprovalService _approvalService;
    private readonly ILogger<PriceSellController> _logger;

    public PriceSellController(
        AppDbContext context,
        SelectListService selectList,
        PriceSellApprovalService approvalService,
        ILogger<PriceSellController> logger)
    {
        _context = context;
        _selectList = selectList;
        _approvalService = approvalService;
        _logger = logger;
    }

public async Task<IActionResult> Index()
{
    ViewBag.PendingApprovalCount = await _context.PriceSellApprovals
        .CountAsync(x => x.Status == "PENDING");

    var priceSells = await _context.PriceSells.ToListAsync();

    return View(priceSells);
}

    public IActionResult Form(int? id)
    {
        ViewBag.ListCustomer = _selectList.getCustomerMains();
        ViewBag.ListOrigin = _selectList.GetOrigins();
        ViewBag.ListDestination = _selectList.GetDestinations();
        ViewBag.ListServiceType = _selectList.GetServiceTypes();
        ViewBag.ListServiceModa = _selectList.GetServiceModas();
        ViewBag.ListTruckSize = _selectList.GetTruckSizes();
        ViewBag.ListChargeUom = _selectList.GetChargeUoms();
        ViewBag.ListCurrency = _selectList.GetCurrency();

        var model = id == null || id == 0
                ? new PriceSell()
                : _context.PriceSells.FirstOrDefault(x => x.id_seq == id) ?? new PriceSell();

        return PartialView("_Form", model);
    }

    // Create: form must post here when id_seq == 0.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("pricesell_create")]
    public async Task<IActionResult> SubmitCreate(PriceSell model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFormListsAsync();
            return PartialView("_Form", model);
        }

        if (model.id_seq != 0)
            return BadRequest(new { success = false, message = "Create tidak menerima id_seq." });

        try
        {
            var dto = new PriceSellApprovalSubmitDto
            {
                RequestType = "CREATE",
                Items = new List<PriceSellApprovalItemDto>
                {
                    new()
                    {
                        OperationType = "CREATE",
                        NewValuesJson = JsonSerializer.Serialize(model)
                    }
                }
            };

            var approvalId = await _approvalService.SubmitAsync(
                dto, HttpContext.Session.GetString("username") ?? "", ct);
            TempData["SuccessMessage"] = $"Request approval berhasil diajukan (ID: {approvalId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateFormListsAsync();
            return PartialView("_Form", model);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Tarif konflik dengan data atau request approval pending lain.");
            await PopulateFormListsAsync();
            return PartialView("_Form", model);
        }
    }

    // Update: form must post here when id_seq > 0.
    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //[RequirePermission("pricesell_update")]
    //public async Task<IActionResult> SubmitUpdate(PriceSell model, CancellationToken ct)
    //{
    //    if (!ModelState.IsValid)
    //    {
    //        await PopulateFormListsAsync();
    //        return PartialView("_Form", model);
    //    }

    //    if (model.id_seq <= 0)
    //        return BadRequest(new { success = false, message = "id_seq wajib untuk update." });

    //    var existing = await _context.PriceSells.AsNoTracking()
    //        .FirstOrDefaultAsync(x => x.id_seq == model.id_seq, ct);
    //    if (existing == null) return NotFound();

    //    try
    //    {
    //        var dto = new PriceSellApprovalSubmitDto
    //        {
    //            RequestType = "UPDATE",
    //            Items = new List<PriceSellApprovalItemDto>
    //            {
    //                new()
    //                {
    //                    OperationType = "UPDATE",
    //                    PriceSellId = existing.id_seq,
    //                    NewValuesJson = JsonSerializer.Serialize(model)
    //                }
    //            }
    //        };

    //        var approvalId = await _approvalService.SubmitAsync(
    //            dto, HttpContext.Session.GetString("username") ?? "", ct);
    //        TempData["SuccessMessage"] = $"Request update approval berhasil diajukan (ID: {approvalId}).";
    //        return RedirectToAction(nameof(Index));
    //    }
    //    catch (InvalidOperationException ex)
    //    {
    //        ModelState.AddModelError(string.Empty, ex.Message);
    //        await PopulateFormListsAsync();
    //        return PartialView("_Form", model);
    //    }
    //    catch (DbUpdateException)
    //    {
    //        ModelState.AddModelError(string.Empty, "Tarif konflik dengan request approval pending lain.");
    //        await PopulateFormListsAsync();
    //        return PartialView("_Form", model);
    //    }
    //}

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("pricesell_update")]
    public async Task<IActionResult> SubmitUpdate(
    PriceSell model,
    CancellationToken ct)
    {
        // 1. Validate the session
        var username = HttpContext.Session.GetString("username");

        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Your session has expired. Please log in again."
            });
        }

        // 2. Validate the submitted ID
        if (model.id_seq <= 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "A valid tariff ID is required for an update."
            });
        }

        // 3. Validate the submitted model
        if (!ModelState.IsValid)
        {
            await PopulateFormListsAsync();

            Response.StatusCode = StatusCodes.Status400BadRequest;
            return PartialView("_Form", model);
        }

        try
        {
            // 4. Verify that the existing tariff still exists
            var existing = await _context.PriceSells
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.id_seq == model.id_seq,
                    ct);

            if (existing == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "The tariff you are trying to update was not found. Please refresh the page."
                });
            }

            // 5. Create the approval request
            var dto = new PriceSellApprovalSubmitDto
            {
                RequestType = "UPDATE",
                Items = new List<PriceSellApprovalItemDto>
            {
                new()
                {
                    OperationType = "UPDATE",
                    PriceSellId = existing.id_seq,
                    NewValuesJson = JsonSerializer.Serialize(model)
                }
            }
            };

            var approvalId = await _approvalService.SubmitAsync(
                dto,
                username,
                ct);

            _logger.LogInformation(
                "PriceSell update approval submitted. ApprovalId: {ApprovalId}, TariffId: {TariffId}, User: {Username}",
                approvalId,
                existing.id_seq,
                username);

            TempData["SuccessMessage"] =
                "Your tariff update request has been submitted successfully and is awaiting approval.";

            return RedirectToAction(nameof(Index));
        }

        catch (InvalidOperationException ex)
        {
            // Business validation or approval conflict.
            _logger.LogWarning(
                ex,
                "PriceSell update request rejected. TariffId: {TariffId}, User: {Username}",
                model.id_seq,
                username);

            ModelState.AddModelError(string.Empty, ex.Message);

            await PopulateFormListsAsync();

            Response.StatusCode = StatusCodes.Status409Conflict;

            return PartialView("_Form", model);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Prevent duplicate pending approval requests.
            _logger.LogWarning(
                ex,
                "PriceSell update approval conflicts with an existing request. TariffId: {TariffId}",
                model.id_seq);

            ModelState.AddModelError(
                string.Empty,
                "A pending approval request already exists for this tariff. Please review the existing request.");

            await PopulateFormListsAsync();

            Response.StatusCode = StatusCodes.Status409Conflict;

            return PartialView("_Form", model);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Database error while submitting PriceSell update. TariffId: {TariffId}",
                model.id_seq);

            ModelState.AddModelError(
                string.Empty,
                "The request could not be saved because of a database error. Please try again or contact support.");

            await PopulateFormListsAsync();

            Response.StatusCode = StatusCodes.Status500InternalServerError;

            return PartialView("_Form", model);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Propagate HTTP request cancellation.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while submitting PriceSell update. TariffId: {TariffId}",
                model.id_seq);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while submitting your request. Please try again later.");

            await PopulateFormListsAsync();

            Response.StatusCode = StatusCodes.Status500InternalServerError;

            return PartialView("_Form", model);
        }

    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.GetBaseException() is SqlException sqlException
            && (sqlException.Number == 2601 || sqlException.Number == 2627);
    }

    // Keep the old URL fail-closed so stale clients cannot write directly.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save() => BadRequest(new
    {
        success = false,
        message = "Endpoint Save lama dinonaktifkan. Form harus menggunakan SubmitCreate atau SubmitUpdate."
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("pricesell_deactivate")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var model = await _context.PriceSells.AsNoTracking()
            .FirstOrDefaultAsync(x => x.id_seq == id, ct);
        if (model == null) return NotFound();

        try
        {
            var dto = new PriceSellApprovalSubmitDto
            {
                RequestType = "DEACTIVATE",
                Items = new List<PriceSellApprovalItemDto>
                {
                    new()
                    {
                        OperationType = "DEACTIVATE",
                        PriceSellId = model.id_seq,
                        NewValuesJson = JsonSerializer.Serialize(model)
                    }
                }
            };

            var approvalId = await _approvalService.SubmitAsync(
                dto, HttpContext.Session.GetString("username") ?? "", ct);
            return Ok(new
            {
                success = true,
                message = "Request deactivation berhasil diajukan untuk approval.",
                approvalId
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { success = false, message = "Tarif sudah memiliki request approval pending." });
        }
    }

    private Task PopulateFormListsAsync()
    {
        ViewBag.ListCustomer = _selectList.getCustomerMains();
        ViewBag.ListOrigin = _selectList.GetOrigins();
        ViewBag.ListDestination = _selectList.GetDestinations();
        ViewBag.ListServiceType = _selectList.GetServiceTypes();
        ViewBag.ListServiceModa = _selectList.GetServiceModas();
        ViewBag.ListTruckSize = _selectList.GetTruckSizes();
        ViewBag.ListChargeUom = _selectList.GetChargeUoms();
        ViewBag.ListCurrency = _selectList.GetCurrency();
        return Task.CompletedTask;
    }

    public IActionResult Upload()
    {
        return View();
    }

    [HttpGet]
    public IActionResult DownloadTemplate()
    {
        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Template");
        var headers = new[]
        {
            "cust_code", "origin", "dest", "serv_type", "serv_moda",
            "truck_size", "charge_uom", "flag_min", "charge_min",
            "flag_range", "min_range", "max_range",
            "sell1", "sell2", "sell3", "sell_ret_empty", "sell_ret_cargo",
            "sell_ovnight", "sell_cancel", "selltrip2", "selltrip3", "sell_diff_area",
            "valid_date", "active_flag", "curr", "rate_value"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Column(i + 1).AdjustToContents();
        }

        worksheet.Cell(2, 1).Value = "CUST001"; // sample data
        worksheet.Cell(2, 2).Value = "JAKARTA";
        worksheet.Cell(2, 3).Value = "BANDUNG";
        worksheet.Cell(2, 4).Value = "REG";
        worksheet.Cell(2, 5).Value = "TRUCK";
        worksheet.Cell(2, 6).Value = "CDE";
        worksheet.Cell(2, 7).Value = "KG";
        worksheet.Cell(2, 8).Value = 1;
        worksheet.Cell(2, 9).Value = 55000;
        worksheet.Cell(2, 10).Value = 1;
        worksheet.Cell(2, 11).Value = 0;
        worksheet.Cell(2, 12).Value = 100;
        worksheet.Cell(2, 13).Value = 70000;
        worksheet.Cell(2, 14).Value = 75000;
        worksheet.Cell(2, 15).Value = 80000;
        worksheet.Cell(2, 16).Value = 35000;
        worksheet.Cell(2, 17).Value = 45000;
        worksheet.Cell(2, 18).Value = 12000;
        worksheet.Cell(2, 19).Value = 1000;
        worksheet.Cell(2, 20).Value = 2000;
        worksheet.Cell(2, 21).Value = 6000;
        worksheet.Cell(2, 22).Value = 8000;
        worksheet.Cell(2, 23).Value = DateTime.Today;
        worksheet.Cell(2, 24).Value = 1;
        worksheet.Cell(2, 25).Value = "IDR";
        worksheet.Cell(2, 26).Value = 1;

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Template_Upload_PriceSell.xlsx");
    }

    private void CreateMasterSheet(XLWorkbook workbook, string sheetName, string[] values)
    {
        var sheet = workbook.Worksheets.Add(sheetName);
        sheet.Cell(1, 1).Value = sheetName;
        sheet.Cell(1, 1).Style.Font.Bold = true;

        for (int i = 0; i < values.Length; i++)
        {
            sheet.Cell(i + 2, 1).Value = values[i];
        }

        sheet.Columns().AdjustToContents();
    }

    private async Task<(bool isValid, string? errorMessage)> ValidatePriceSellItemAsync(PriceSellDto item)
    {
        if (!await _context.CustomerMains.AnyAsync(c => c.MAIN_CUST == item.cust_code))
            return (false, $"Main customer code is invalid: {item.cust_code}");

        if (!await _context.Origins.AnyAsync(l => l.origin_code == item.origin))
            return (false, $"Origin is invalid: {item.origin}");

        if (!await _context.Destinations.AnyAsync(l => l.destination_code == item.dest))
            return (false, $"Destination is invalid: {item.dest}");

        if (!await _context.ServiceTypes.AnyAsync(s => s.serv_name == item.serv_type))
            return (false, $"Service Type is invalid: {item.serv_type}");

        if (!await _context.ServiceModas.AnyAsync(s => s.moda_name == item.serv_moda))
            return (false, $"Service Moda is invalid: {item.serv_moda}");

        if (!await _context.TruckSizes.AnyAsync(t => t.trucksize_code == item.truck_size))
            return (false, $"Truck Size is invalid: {item.truck_size}");

        if (!await _context.ChargeUoms.AnyAsync(c => c.charge_name == item.charge_uom))
            return (false, $"UOM is invalid: {item.charge_uom}");

        return (true, null);
    }

    //[HttpPost]
    //public async Task<IActionResult> UploadExcel([FromBody] UploadPriceSellRequest request)
    //{
    //    if (!ModelState.IsValid)
    //    {
    //        var errors = ModelState
    //            .Where(e => e.Value?.Errors.Count > 0)
    //            .ToDictionary(
    //                kvp => kvp.Key,
    //                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
    //            );

    //        return BadRequest(new { message = "Model binding failed.", errors });
    //    }

    //    if (request?.data == null || !request.data.Any())
    //        return BadRequest(new { message = "Data is empty or unreadable." });

    //    // VALIDASI DUPLIKAT DI FILE
    //    var duplicates = request.data
    //        .GroupBy(x => new { x.cust_code, x.origin, x.dest, x.serv_type, x.serv_moda, x.truck_size, x.charge_uom })
    //        .Where(g => g.Count() > 1)
    //        .Select(g => $"Duplicate data found: {g.Key.cust_code} | {g.Key.origin} | {g.Key.dest} | {g.Key.serv_type} | {g.Key.serv_moda} | {g.Key.truck_size} | {g.Key.charge_uom}")
    //        .ToList();

    //    if (duplicates.Any())
    //        return BadRequest(new { message = "Duplicate data found in the uploaded file.", errors = duplicates });

    //    // VALIDASI MODE
    //    var modeErrors = new List<string>();

    //    foreach (var item in request.data)
    //    {
    //        var query = _context.PriceSells.Where(x =>
    //            x.cust_code == item.cust_code &&
    //            x.origin == item.origin &&
    //            x.dest == item.dest &&
    //            x.serv_type == item.serv_type &&
    //            x.serv_moda == item.serv_moda &&
    //            x.truck_size == item.truck_size &&
    //            x.charge_uom == item.charge_uom
    //        );

    //        var exists = await query.AnyAsync();

    //        if (request.mode == "add" && exists)
    //            modeErrors.Add($"(ADD) Data already exists: {item.cust_code} | {item.origin} | {item.dest} | {item.serv_type} | {item.serv_moda} | {item.truck_size} | {item.charge_uom}");

    //        if (request.mode == "edit" && !exists)
    //            modeErrors.Add($"(EDIT) Data not found: {item.cust_code} | {item.origin} | {item.dest} | {item.serv_type} | {item.serv_moda} | {item.truck_size} | {item.charge_uom}");
    //    }

    //    if (modeErrors.Any())
    //        return BadRequest(new { message = "Validation based on mode failed.", errors = modeErrors });

    //    // VALIDASI MASTER
    //    var validationErrors = new List<string>();
    //    foreach (var item in request.data)
    //    {
    //        var (isValid, errorMessage) = await ValidatePriceSellItemAsync(item);
    //        if (!isValid && errorMessage != null)
    //            validationErrors.Add($"[{item.cust_code}-{item.origin}-{item.dest}] {errorMessage}");
    //    }

    //    if (validationErrors.Any())
    //        return BadRequest(new { message = "Master validation failed.", errors = validationErrors });

    //    // INSERT / UPDATE
    //    foreach (var item in request.data)
    //    {
    //        var entity = new PriceSell
    //        {
    //            cust_code = item.cust_code,
    //            origin = item.origin,
    //            dest = item.dest,
    //            serv_type = item.serv_type,
    //            serv_moda = item.serv_moda,
    //            truck_size = item.truck_size,
    //            charge_uom = item.charge_uom,
    //            flag_min = item.flag_min.HasValue ? (byte?)item.flag_min : null,
    //            charge_min = item.charge_min,
    //            flag_range = item.flag_range.HasValue ? (byte?)item.flag_range : null,
    //            min_range = item.min_range,
    //            max_range = item.max_range,
    //            sell1 = item.sell1,
    //            sell2 = item.sell2,
    //            sell3 = item.sell3,
    //            sell_ret_empty = item.sell_ret_empty,
    //            sell_ret_cargo = item.sell_ret_cargo,
    //            sell_ovnight = item.sell_ovnight,
    //            sell_cancel = item.sell_cancel,
    //            selltrip2 = item.selltrip2,
    //            selltrip3 = item.selltrip3,
    //            sell_diff_area = item.sell_diff_area,
    //            valid_date = item.valid_date,
    //            active_flag = item.active_flag,
    //            curr = item.curr,
    //            rate_value = item.rate_value,
    //            entry_user = HttpContext.Session.GetString("username") ?? "System",
    //            entry_date = DateTime.Now
    //        };

    //        if (request.mode == "edit")
    //        {
    //            var existing = await _context.PriceSells.FirstOrDefaultAsync(x =>
    //                x.cust_code == item.cust_code &&
    //                x.origin == item.origin &&
    //                x.dest == item.dest &&
    //                x.serv_type == item.serv_type &&
    //                x.serv_moda == item.serv_moda &&
    //                x.truck_size == item.truck_size &&
    //                x.charge_uom == item.charge_uom
    //            );

    //            if (existing != null)
    //            {
    //                // Update all fields
    //                existing.flag_min = entity.flag_min;
    //                existing.charge_min = entity.charge_min;
    //                existing.flag_range = entity.flag_range;
    //                existing.min_range = entity.min_range;
    //                existing.max_range = entity.max_range;
    //                existing.sell1 = entity.sell1;
    //                existing.sell2 = entity.sell2;
    //                existing.sell3 = entity.sell3;
    //                existing.sell_ret_empty = entity.sell_ret_empty;
    //                existing.sell_ret_cargo = entity.sell_ret_cargo;
    //                existing.sell_ovnight = entity.sell_ovnight;
    //                existing.sell_cancel = entity.sell_cancel;
    //                existing.selltrip2 = entity.selltrip2;
    //                existing.selltrip3 = entity.selltrip3;
    //                existing.sell_diff_area = entity.sell_diff_area;
    //                existing.valid_date = entity.valid_date;
    //                existing.active_flag = entity.active_flag;
    //                existing.curr = entity.curr;
    //                existing.rate_value = entity.rate_value;
    //                existing.update_user = HttpContext.Session.GetString("username") ?? "System";
    //                existing.update_date = DateTime.Now;

    //                continue; // skip insert
    //            }
    //        }

    //        // Tambah baru
    //        _context.PriceSells.Add(entity);
    //    }

    //    await _context.SaveChangesAsync();

    //    return Ok(new { message = "PriceSell data processed successfully." });
    //}

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("pricesell_import")]
    public async Task<IActionResult> UploadExcel([FromBody] UploadPriceSellRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Model binding failed." });

        if (request?.data == null || request.data.Count == 0)
            return BadRequest(new { message = "Data kosong atau tidak terbaca." });

        var errors = new List<string>();
        var skipped = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var approvalItems = new List<PriceSellApprovalItemDto>();
        var mode = (request.mode ?? "").Trim().ToLowerInvariant();
        if (mode is not ("add" or "edit"))
            return BadRequest(new { message = "Mode harus add atau edit." });

        var username = HttpContext.Session.GetString("username") ?? "";
        foreach (var item in request.data)
        {
            var key = $"{item.cust_code}|{item.origin}|{item.dest}|{item.serv_type}|{item.serv_moda}|{item.truck_size}|{item.charge_uom}";
            var label = key.Replace("|", " › ");
            if (!seen.Add(key))
            {
                errors.Add($"[Duplikat di file] {label}");
                continue;
            }

            var (isValid, errorMessage) = await ValidatePriceSellItemAsync(item);
            if (!isValid)
            {
                errors.Add($"[Validasi master] {label} — {errorMessage}");
                continue;
            }

            var existing = await _context.PriceSells.FirstOrDefaultAsync(x =>
                x.cust_code == item.cust_code && x.origin == item.origin &&
                x.dest == item.dest && x.serv_type == item.serv_type &&
                x.serv_moda == item.serv_moda && x.truck_size == item.truck_size &&
                x.charge_uom == item.charge_uom, ct);

            if (mode == "add" && existing != null)
            {
                // Preserve the existing ADD behavior: already-existing tariffs are skipped.
                skipped.Add(label);
                continue;
            }
            if (mode == "edit" && existing == null)
            {
                errors.Add($"[Tidak ditemukan untuk edit] {label}");
                continue;
            }

            var entity = new PriceSell
            {
                cust_code = item.cust_code,
                origin = item.origin,
                dest = item.dest,
                serv_type = item.serv_type,
                serv_moda = item.serv_moda,
                truck_size = item.truck_size,
                charge_uom = item.charge_uom,
                flag_min = item.flag_min.HasValue ? (byte?)item.flag_min : null,
                charge_min = item.charge_min,
                flag_range = item.flag_range.HasValue ? (byte?)item.flag_range : null,
                min_range = item.min_range,
                max_range = item.max_range,
                sell1 = item.sell1,
                sell2 = item.sell2,
                sell3 = item.sell3,
                sell_ret_empty = item.sell_ret_empty,
                sell_ret_cargo = item.sell_ret_cargo,
                sell_ovnight = item.sell_ovnight,
                sell_cancel = item.sell_cancel,
                selltrip2 = item.selltrip2,
                selltrip3 = item.selltrip3,
                sell_diff_area = item.sell_diff_area,
                valid_date = item.valid_date,
                active_flag = item.active_flag,
                curr = item.curr,
                rate_value = item.rate_value,
                entry_user = username,
                entry_date = DateTime.Now
            };

            approvalItems.Add(new PriceSellApprovalItemDto
            {
                OperationType = mode == "add" ? "CREATE" : "UPDATE",
                PriceSellId = existing?.id_seq,
                NewValuesJson = JsonSerializer.Serialize(entity)
            });
        }

        // All-or-nothing: no approval request is created if any row has an error.
        if (errors.Count > 0)
            return BadRequest(new
            {
                message = "Import dibatalkan. Ada baris yang tidak valid; tidak ada request approval yang dibuat.",
                errors,
                skipped
            });

        if (approvalItems.Count == 0)
            return Ok(new
            {
                message = "Tidak ada baris baru untuk diajukan. Semua baris mungkin sudah ada.",
                approvalId = (long?)null,
                skipped
            });

        try
        {
            var dto = new PriceSellApprovalSubmitDto
            {
                RequestType = "IMPORT",
                SourceFileName = "PriceSell upload",
                Items = approvalItems
            };
            var approvalId = await _approvalService.SubmitAsync(dto, username, ct);
            return Ok(new
            {
                success = true,
                message = $"Import diajukan untuk approval: {approvalItems.Count} baris.",
                approvalId,
                submitted = approvalItems.Count,
                skipped
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message, skipped });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { success = false, message = "Import konflik dengan request pending lain atau data yang sudah ada.", skipped });
        }
    }

    [HttpGet]
    public IActionResult DownloadExcel(string cust)
    {
        // cek admin
        var isAdmin = HttpContext.Session.GetString("is_admin") == "true";
        if (!isAdmin)
            return Unauthorized("Not allowed");

        // ambil list customer
        var selected = new List<string>();
        if (!string.IsNullOrEmpty(cust))
            selected = System.Text.Json.JsonSerializer.Deserialize<List<string>>(cust);

        bool isAll = selected.Count == 0;

        // ambil data dari DB
        var data = isAll
            ? _context.PriceSells.ToList()
            : _context.PriceSells.Where(x => selected.Contains(x.cust_code)).ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("PriceSell");

        // header sama persis
        var headers = new[]
        {
        "cust_code", "origin", "dest", "serv_type", "serv_moda",
        "truck_size", "charge_uom", "flag_min", "charge_min",
        "flag_range", "min_range", "max_range",
        "sell1", "sell2", "sell3", "sell_ret_empty", "sell_ret_cargo",
        "sell_ovnight", "sell_cancel", "selltrip2", "selltrip3", "sell_diff_area",
        "valid_date", "active_flag", "curr", "rate_value"
    };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        int row = 2;

        foreach (var item in data)
        {
            ws.Cell(row, 1).Value = item.cust_code;
            ws.Cell(row, 2).Value = item.origin;
            ws.Cell(row, 3).Value = item.dest;
            ws.Cell(row, 4).Value = item.serv_type;
            ws.Cell(row, 5).Value = item.serv_moda;
            ws.Cell(row, 6).Value = item.truck_size;
            ws.Cell(row, 7).Value = item.charge_uom;
            ws.Cell(row, 8).Value = item.flag_min;
            ws.Cell(row, 9).Value = item.charge_min;
            ws.Cell(row, 10).Value = item.flag_range;
            ws.Cell(row, 11).Value = item.min_range;
            ws.Cell(row, 12).Value = item.max_range;
            ws.Cell(row, 13).Value = item.sell1;
            ws.Cell(row, 14).Value = item.sell2;
            ws.Cell(row, 15).Value = item.sell3;
            ws.Cell(row, 16).Value = item.sell_ret_empty;
            ws.Cell(row, 17).Value = item.sell_ret_cargo;
            ws.Cell(row, 18).Value = item.sell_ovnight;
            ws.Cell(row, 19).Value = item.sell_cancel;
            ws.Cell(row, 20).Value = item.selltrip2;
            ws.Cell(row, 21).Value = item.selltrip3;
            ws.Cell(row, 22).Value = item.sell_diff_area;
            ws.Cell(row, 23).Value = item.valid_date;
            ws.Cell(row, 24).Value = item.active_flag;
            ws.Cell(row, 25).Value = item.curr;
            ws.Cell(row, 26).Value = item.rate_value;

            row++;
        }

        ws.Columns().AdjustToContents();

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        string label = isAll ? "ALL" : string.Join("-", selected);

        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"PriceSell_{label}.xlsx");
    }


}
