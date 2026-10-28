using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Models.UploadModels;
using TMSBilling.Services;
using System.Text.Json;
namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class PriceBuyController : Controller
    {
        private readonly AppDbContext _context;
        private readonly SelectListService _selectList;
        private readonly PriceBuyApprovalService _approvalService;

        public PriceBuyController(AppDbContext context, SelectListService selectList, PriceBuyApprovalService approvalService)
        {
            _context = context;
            _selectList = selectList;
            _approvalService = approvalService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.PendingApprovalCount = await _context.PriceBuyApprovals.CountAsync(x => x.Status == "PENDING");
            var data = await _context.PriceBuys.OrderByDescending(x => x.entry_date).ToListAsync();
            return View(data);
        }

        public IActionResult Form(int? id)
        {

            ViewBag.ListVendor = _selectList.GetVendors();
            ViewBag.ListCurrency = _selectList.GetCurrency();
            ViewBag.ListOrigin = _selectList.GetOrigins();
            ViewBag.ListDestination = _selectList.GetDestinations();
            ViewBag.ListServiceType = _selectList.GetServiceTypes();
            ViewBag.ListServiceModa = _selectList.GetServiceModas();
            ViewBag.ListTruckSize = _selectList.GetTruckSizes();
            ViewBag.ListChargeUom = _selectList.GetChargeUoms();

            var model = id == null || id == 0
                ? new PriceBuy()
                : _context.PriceBuys.FirstOrDefault(x => x.id_seq == id) ?? new PriceBuy();

            return PartialView("_Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("pricebuy_create")]
        public async Task<IActionResult> SubmitCreate(PriceBuy model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                PopulateFormLists();
                return PartialView("_Form", model);
            }
            if (model.id_seq != 0)
                return BadRequest(new { success = false, message = "A create request cannot contain an existing tariff ID." });

            try
            {
                model.origin = model.origin?.Trim().ToUpperInvariant();
                model.dest = model.dest?.Trim().ToUpperInvariant();
                model.entry_user = HttpContext.Session.GetString("username") ?? "System";
                var dto = new PriceBuyApprovalSubmitDto {
                    RequestType = "CREATE",
                    Items = new List<PriceBuyApprovalItemDto> {
                        new() { OperationType = "CREATE", NewValuesJson = JsonSerializer.Serialize(model) }
                    }
                };
                var id = await _approvalService.SubmitAsync(dto, HttpContext.Session.GetString("username") ?? "", ct);
                TempData["SuccessMessage"] = $"Create approval request submitted successfully (ID: {id}).";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                PopulateFormLists();
                return PartialView("_Form", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("pricebuy_update")]
        public async Task<IActionResult> SubmitUpdate(PriceBuy model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                PopulateFormLists();
                return PartialView("_Form", model);
            }
            if (model.id_seq <= 0)
                return BadRequest(new { success = false, message = "A valid tariff ID is required for an update request." });

            var existing = await _context.PriceBuys.AsNoTracking().FirstOrDefaultAsync(x => x.id_seq == model.id_seq, ct);
            if (existing == null) return NotFound();
            try
            {
                model.sup_code = existing.sup_code;
                model.origin = existing.origin;
                model.dest = existing.dest;
                model.serv_type = existing.serv_type;
                model.serv_moda = existing.serv_moda;
                model.truck_size = existing.truck_size;
                model.charge_uom = existing.charge_uom;
                model.update_user = HttpContext.Session.GetString("username") ?? "System";
                var dto = new PriceBuyApprovalSubmitDto {
                    RequestType = "UPDATE",
                    Items = new List<PriceBuyApprovalItemDto> {
                        new() { OperationType = "UPDATE", PriceBuyId = existing.id_seq, NewValuesJson = JsonSerializer.Serialize(model) }
                    }
                };
                var id = await _approvalService.SubmitAsync(dto, HttpContext.Session.GetString("username") ?? "", ct);
                TempData["SuccessMessage"] = $"Update approval request submitted successfully (ID: {id}).";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                PopulateFormLists();
                return PartialView("_Form", model);
            }
        }

        // Legacy writes are disabled to ensure all tariff changes go through approval.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save() => BadRequest(new { success = false, message = "The legacy Save endpoint is disabled. Please submit an approval request." });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("pricebuy_deactivate")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var existing = await _context.PriceBuys.AsNoTracking().FirstOrDefaultAsync(x => x.id_seq == id, ct);
            if (existing == null) return NotFound();
            try
            {
                var proposed = existing;
                proposed.active_flag = 0;
                var dto = new PriceBuyApprovalSubmitDto {
                    RequestType = "DEACTIVATE",
                    Items = new List<PriceBuyApprovalItemDto> {
                        new() { OperationType = "DEACTIVATE", PriceBuyId = existing.id_seq, NewValuesJson = JsonSerializer.Serialize(proposed) }
                    }
                };
                var idApproval = await _approvalService.SubmitAsync(dto, HttpContext.Session.GetString("username") ?? "", ct);
                return Ok(new { success = true, approvalId = idApproval, message = "Deactivation request submitted successfully and is pending approval." });
            }
            catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
        }

        private void PopulateFormLists()
        {
            ViewBag.ListVendor = _selectList.GetVendors();
            ViewBag.ListCurrency = _selectList.GetCurrency();
            ViewBag.ListOrigin = _selectList.GetOrigins();
            ViewBag.ListDestination = _selectList.GetDestinations();
            ViewBag.ListServiceType = _selectList.GetServiceTypes();
            ViewBag.ListServiceModa = _selectList.GetServiceModas();
            ViewBag.ListTruckSize = _selectList.GetTruckSizes();
            ViewBag.ListChargeUom = _selectList.GetChargeUoms();
        }

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Template");
            var headers = new[]
            {
                "sup_code", "origin", "dest", "serv_type", "serv_moda",
                "truck_size", "charge_uom", "flag_min", "charge_min",
                "flag_range", "min_range", "max_range",
                "buy1", "buy2", "buy3", "buy_ret_empt", "buy_ret_cargo",
                "buy_ovnight", "buy_cancel", "buytrip2", "buytrip3", "buy_diff_area",
                "valid_date", "active_flag", "curr", "rate_value"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Column(i + 1).AdjustToContents();
            }

            worksheet.Cell(2, 1).Value = "SUP001"; // sample data...
            worksheet.Cell(2, 2).Value = "JAKARTA";
            worksheet.Cell(2, 3).Value = "BANDUNG";
            worksheet.Cell(2, 4).Value = "REG";
            worksheet.Cell(2, 5).Value = "TRUCK";
            worksheet.Cell(2, 6).Value = "CDE";
            worksheet.Cell(2, 7).Value = "KG";
            worksheet.Cell(2, 8).Value = 1;
            worksheet.Cell(2, 9).Value = 50000;
            worksheet.Cell(2, 10).Value = 1;
            worksheet.Cell(2, 11).Value = 0;
            worksheet.Cell(2, 12).Value = 100;
            worksheet.Cell(2, 13).Value = 60000;
            worksheet.Cell(2, 14).Value = 65000;
            worksheet.Cell(2, 15).Value = 70000;
            worksheet.Cell(2, 16).Value = 30000;
            worksheet.Cell(2, 17).Value = 40000;
            worksheet.Cell(2, 18).Value = 10000;
            worksheet.Cell(2, 19).Value = 0;
            worksheet.Cell(2, 20).Value = 0;
            worksheet.Cell(2, 21).Value = 5000;
            worksheet.Cell(2, 22).Value = 6000;
            worksheet.Cell(2, 23).Value = DateTime.Today;
            worksheet.Cell(2, 24).Value = 1;
            worksheet.Cell(2, 25).Value = "IDR";
            worksheet.Cell(2, 26).Value = 1;


            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Template_Upload_PriceBuy.xlsx");
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

        private async Task<(bool isValid, string? errorMessage)> ValidatePriceBuyItemAsync(PriceBuyDto item)
        {
            if (!await _context.Vendors.AnyAsync(v => v.SUP_CODE == item.sup_code))
                return (false, $"Kode vendor tidak valid: {item.sup_code}");

            if (!await _context.Origins.AnyAsync(l => l.origin_code == item.origin))
                return (false, $"Kode origin tidak valid: {item.origin}");

            if (!await _context.Destinations.AnyAsync(l => l.destination_code == item.dest))
                return (false, $"Kode destination tidak valid: {item.dest}");

            if (!await _context.ServiceTypes.AnyAsync(s => s.serv_name == item.serv_type))
                return (false, $"Tipe layanan tidak valid: {item.serv_type}");

            if (!await _context.ServiceModas.AnyAsync(s => s.moda_name == item.serv_moda))
                return (false, $"Moda layanan tidak valid: {item.serv_moda}");

            if (!await _context.TruckSizes.AnyAsync(t => t.trucksize_code == item.truck_size))
                return (false, $"Ukuran truk tidak valid: {item.truck_size}");

            if (!await _context.ChargeUoms.AnyAsync(c => c.charge_name == item.charge_uom))
                return (false, $"UOM tidak valid: {item.charge_uom}");

            return (true, null);
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("pricebuy_import")]
        public async Task<IActionResult> UploadExcel([FromBody] UploadPriceBuyRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid || request?.data == null || request.data.Count == 0)
                return BadRequest(new { success = false, message = "No valid Excel rows were submitted." });

            var username = HttpContext.Session.GetString("username") ?? "";
            var items = new List<PriceBuyApprovalItemDto>();
            var skipped = new List<string>();
            var errors = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in request.data)
            {
                var label = $"{row.sup_code} | {row.origin} | {row.dest} | {row.serv_type} | {row.serv_moda} | {row.truck_size} | {row.charge_uom}";
                var key = string.Join("|", new[] { row.sup_code, row.origin, row.dest, row.serv_type, row.serv_moda, row.truck_size, row.charge_uom });
                if (!seen.Add(key)) { errors.Add($"Duplicate row in Excel: {label}"); continue; }

                var entity = new PriceBuy {
                    sup_code = row.sup_code?.Trim(),
                    origin = row.origin?.Trim().ToUpperInvariant(),
                    dest = row.dest?.Trim().ToUpperInvariant(),
                    serv_type = row.serv_type?.Trim(),
                    serv_moda = row.serv_moda?.Trim(),
                    truck_size = row.truck_size?.Trim(),
                    charge_uom = row.charge_uom?.Trim(),
                    flag_min = row.flag_min.HasValue ? (byte?)row.flag_min : null,
                    charge_min = row.charge_min,
                    flag_range = row.flag_range.HasValue ? (byte?)row.flag_range : null,
                    min_range = row.min_range, max_range = row.max_range,
                    buy1 = row.buy1, buy2 = row.buy2, buy3 = row.buy3,
                    buy_ret_empt = row.buy_ret_empt, buy_ret_cargo = row.buy_ret_cargo,
                    buy_ovnight = row.buy_ovnight, buy_cancel = row.buy_cancel,
                    buytrip2 = row.buytrip2, buytrip3 = row.buytrip3, buy_diff_area = row.buy_diff_area,
                    valid_date = row.valid_date, active_flag = row.active_flag, curr = row.curr, rate_value = row.rate_value,
                    entry_user = username, entry_date = DateTime.Now
                };

                if (!await _context.Vendors.AnyAsync(v => v.SUP_CODE == entity.sup_code, ct) ||
                    !await _context.Origins.AnyAsync(v => v.origin_code == entity.origin, ct) ||
                    !await _context.Destinations.AnyAsync(v => v.destination_code == entity.dest, ct) ||
                    !await _context.ServiceTypes.AnyAsync(v => v.serv_name == entity.serv_type, ct) ||
                    !await _context.ServiceModas.AnyAsync(v => v.moda_name == entity.serv_moda, ct) ||
                    !await _context.TruckSizes.AnyAsync(v => v.trucksize_code == entity.truck_size, ct) ||
                    !await _context.ChargeUoms.AnyAsync(v => v.charge_name == entity.charge_uom, ct))
                { errors.Add($"Invalid master data: {label}"); continue; }

                var existing = await _context.PriceBuys.FirstOrDefaultAsync(x =>
                    x.sup_code == entity.sup_code && x.origin == entity.origin && x.dest == entity.dest &&
                    x.serv_type == entity.serv_type && x.serv_moda == entity.serv_moda &&
                    x.truck_size == entity.truck_size && x.charge_uom == entity.charge_uom, ct);

                if (string.Equals(request.mode, "add", StringComparison.OrdinalIgnoreCase) && existing != null)
                { skipped.Add(label); continue; }
                if (string.Equals(request.mode, "edit", StringComparison.OrdinalIgnoreCase) && existing == null)
                { errors.Add($"Tariff not found for edit: {label}"); continue; }

                items.Add(new PriceBuyApprovalItemDto {
                    OperationType = existing == null ? "CREATE" : "UPDATE",
                    PriceBuyId = existing?.id_seq,
                    NewValuesJson = JsonSerializer.Serialize(entity)
                });
            }

            if (errors.Count > 0)
                return BadRequest(new { success = false, message = "Import was cancelled because one or more rows are invalid. No approval request was created.", errors, skipped });
            if (items.Count == 0)
                return Ok(new { success = false, message = "No new approval request was created. All rows were skipped.", submitted = 0, skipped });

            try
            {
                var dto = new PriceBuyApprovalSubmitDto { RequestType = "IMPORT", SourceFileName = "PriceBuy Excel import", Items = items };
                var approvalId = await _approvalService.SubmitAsync(dto, username, ct);
                return Ok(new { success = true, message = "Excel import approval request submitted successfully.", approvalId, submitted = items.Count, skipped });
            }
            catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException)
            {
                return Conflict(new { success = false, message = ex.Message, skipped });
            }
        }

        [HttpGet]
        public IActionResult Download([FromQuery] List<string> vendor)
        {
            var isAdmin = HttpContext.Session.GetString("is_admin") == "true";
            if (!isAdmin)
            {
                return Unauthorized("Only admin can download this file.");
            }

            var dataQuery = _context.PriceBuys.AsQueryable();

            // jika vendor list ada maka filter
            if (vendor != null && vendor.Count > 0)
            {
                dataQuery = dataQuery.Where(x => vendor.Contains(x.sup_code));
            }

            var list = dataQuery.ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("PriceBuy");

            // header
            var headers = new[]
            {
                "sup_code", "origin", "dest", "serv_type", "serv_moda",
                "truck_size", "charge_uom", "flag_min", "charge_min",
                "flag_range", "min_range", "max_range",
                "buy1", "buy2", "buy3", "buy_ret_empt", "buy_ret_cargo",
                "buy_ovnight", "buy_cancel", "buytrip2", "buytrip3", "buy_diff_area",
                "valid_date", "active_flag", "curr", "rate_value"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
            }

            // isi data
            int row = 2;
            foreach (var item in list)
            {
                ws.Cell(row, 1).Value = item.sup_code;
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
                ws.Cell(row, 13).Value = item.buy1;
                ws.Cell(row, 14).Value = item.buy2;
                ws.Cell(row, 15).Value = item.buy3;
                ws.Cell(row, 16).Value = item.buy_ret_empt;
                ws.Cell(row, 17).Value = item.buy_ret_cargo;
                ws.Cell(row, 18).Value = item.buy_ovnight;
                ws.Cell(row, 19).Value = item.buy_cancel;
                ws.Cell(row, 20).Value = item.buytrip2;
                ws.Cell(row, 21).Value = item.buytrip3;
                ws.Cell(row, 22).Value = item.buy_diff_area;
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

            var vendorLabel = (vendor == null || vendor.Count == 0)
                ? "ALL"
                : string.Join("-", vendor);


            return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"PriceBuy_{vendorLabel}.xlsx");
        }


    }
}
