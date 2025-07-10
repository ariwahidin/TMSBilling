using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Models.UploadModels;
namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class PriceBuyController : Controller
    {
        private readonly AppDbContext _context;
        private readonly SelectListService _selectList;

        public PriceBuyController(AppDbContext context, SelectListService selectList)
        {
            _context = context;
            _selectList = selectList;
        }

        public IActionResult Index()
        {
            var data = _context.PriceBuys.OrderByDescending(x => x.entry_date).ToList();
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
        public IActionResult Save(PriceBuy model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Index");

            if (model.id_seq == 0)
            {
                model.entry_date = DateTime.Now;
                model.entry_user = HttpContext.Session.GetString("username") ?? "System";
                _context.PriceBuys.Add(model);
            }
            else
            {
                var existing = _context.PriceBuys.FirstOrDefault(x => x.id_seq == model.id_seq);
                if (existing == null) return NotFound();

                // Map properti
                existing.sup_code = model.sup_code;
                existing.origin = model.origin;
                existing.dest = model.dest;
                existing.serv_type = model.serv_type;
                existing.serv_moda = model.serv_moda;
                existing.truck_size = model.truck_size;
                existing.charge_uom = model.charge_uom;
                existing.flag_min = model.flag_min;
                existing.charge_min = model.charge_min;
                existing.flag_range = model.flag_range;
                existing.min_range = model.min_range;
                existing.max_range = model.max_range;
                existing.buy1 = model.buy1;
                existing.buy2 = model.buy2;
                existing.buy3 = model.buy3;
                existing.buy_ret_empt = model.buy_ret_empt;
                existing.buy_ret_cargo = model.buy_ret_cargo;
                existing.buy_ovnight = model.buy_ovnight;
                existing.buy_cancel = model.buy_cancel;
                existing.buytrip2 = model.buytrip2;
                existing.buytrip3 = model.buytrip3;
                existing.buy_diff_area = model.buy_diff_area;
                existing.valid_date = model.valid_date;
                existing.curr = model.curr;
                existing.rate_value = model.rate_value;
                existing.active_flag = model.active_flag;
                existing.update_date = DateTime.Now;
                existing.update_user = HttpContext.Session.GetString("username") ?? "System";
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = _context.PriceBuys.Find(id);
            if (item == null) return NotFound();

            _context.PriceBuys.Remove(item);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }


        public IActionResult Upload()
        {
            return View();
        }


        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();

            // 1. Sheet Template
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

            // 2. Sheet Master - 1 sheet 1 field
            //CreateMasterSheet(workbook, "SupCode",
            //    _context.Vendors.Select(v => v.SUP_CODE).Distinct().ToArray());

            //CreateMasterSheet(workbook, "Origin",
            //    _context.Origins.Select(l => l.origin_code).Distinct().ToArray());

            //CreateMasterSheet(workbook, "Dest",
            //    _context.Destinations.Select(l => l.destination_code).Distinct().ToArray());

            //CreateMasterSheet(workbook, "ServType",
            //    _context.ServiceTypes.Select(s => s.serv_name).Distinct().ToArray());

            //CreateMasterSheet(workbook, "ServModa",
            //    _context.ServiceModas.Select(s => s.moda_name).Distinct().ToArray());

            //CreateMasterSheet(workbook, "TruckSize",
            //    _context.TruckSizes.Select(t => t.trucksize_code).Distinct().ToArray());

            //CreateMasterSheet(workbook, "ChargeUom",
            //    _context.ChargeUoms.Select(u => u.charge_name).Distinct().ToArray());


            // 3. Output file
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Template_Upload_PriceBuy.xlsx");
        }

        // Fungsi bantu untuk bikin sheet master
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

        [HttpPost]
        public async Task<IActionResult> UploadExcel([FromBody] UploadPriceBuyRequest request)
        {

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new { message = "Model binding gagal.", errors });
            }

            if (request?.data == null || !request.data.Any())
                return BadRequest(new { message = "Data kosong atau tidak bisa dibaca." });


            // TAHAP VALIDASI DUPLIKAT ROWS DI FILE EXCELNYA
            var duplicates = request.data
                .GroupBy(x => new { x.sup_code, x.origin, x.dest, x.serv_type, x.serv_moda, x.truck_size, x.charge_uom })
                .Where(g => g.Count() > 1)
                .Select(g => $"Data duplikat ditemukan untuk kombinasi: {g.Key.sup_code} | {g.Key.origin} | {g.Key.dest} | {g.Key.serv_type} | {g.Key.serv_moda} | {g.Key.truck_size} | {g.Key.charge_uom}")
                .ToList();

            if (duplicates.Any())
            {
                return BadRequest(new
                {
                    message = "Terdapat data duplikat dalam file yang diupload.",
                    errors = duplicates
                });
            }


            // Validasi berdasarkan MODE ADD/EDIT terhadap data di database
            var modeErrors = new List<string>();

            foreach (var item in request.data)
            {

                var query = _context.PriceBuys.Where(x =>
                    x.sup_code == item.sup_code &&
                    x.origin == item.origin &&
                    x.dest == item.dest &&
                    x.serv_type == item.serv_type &&
                    x.serv_moda == item.serv_moda &&
                    x.truck_size == item.truck_size &&
                    x.charge_uom == item.charge_uom
                );

                Console.WriteLine(query.ToQueryString()); // tampilkan SQL di console/log

                var exists = await query.AnyAsync();

                if (request.mode == "add" && exists)
                {
                    modeErrors.Add($"(ADD) Data sudah ada: {item.sup_code} | {item.origin} | {item.dest} | {item.serv_type} | {item.serv_moda} | {item.truck_size} | {item.charge_uom}");
                }

                if (request.mode == "edit" && !exists)
                {
                    modeErrors.Add($"(EDIT) Data tidaks ditemukan: {item.sup_code} | {item.origin} | {item.dest} | {item.serv_type} | {item.serv_moda} | {item.truck_size} | {item.charge_uom}");
                }
            }

            if (modeErrors.Any())
            {
                return BadRequest(new { message = "Validasi berdasarkan mode gagal.", errors = modeErrors });
            }



            // TAHAP VALIDASI MASTER
            var validationErrors = new List<string>();
            foreach (var item in request.data)
            {
                var (isValid, errorMessage) = await ValidatePriceBuyItemAsync(item);
                if (!isValid && errorMessage != null)
                {
                    validationErrors.Add($"[{item.sup_code}-{item.origin}-{item.dest}] {errorMessage}");
                }
            }

            if (validationErrors.Any())
            {
                return BadRequest(new { message = "Validasi data gagal.", errors = validationErrors });
            }


            // TAHAP INSERT / UPDATE
            foreach (var item in request.data)
            {
                // Buat entitas baru dari item
                var entity = new PriceBuy
                {
                    sup_code = item.sup_code,
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
                    buy1 = item.buy1,
                    buy2 = item.buy2,
                    buy3 = item.buy3,
                    buy_ret_empt = item.buy_ret_empt,
                    buy_ret_cargo = item.buy_ret_cargo,
                    buy_ovnight = item.buy_ovnight,
                    buy_cancel = item.buy_cancel,
                    buytrip2 = item.buytrip2,
                    buytrip3 = item.buytrip3,
                    buy_diff_area = item.buy_diff_area,
                    valid_date = item.valid_date,
                    active_flag = item.active_flag,
                    curr = item.curr,
                    rate_value = item.rate_value,
                    entry_user = HttpContext.Session.GetString("username") ?? "System",
                    entry_date = DateTime.Now
                };

                if (request.mode == "edit")
                {
                    // Cari data lama berdasarkan kombinasi kunci logis (bukan id_seq)
                    var existing = await _context.PriceBuys.FirstOrDefaultAsync(x =>
                        x.sup_code == item.sup_code &&
                        x.origin == item.origin &&
                        x.dest == item.dest &&
                        x.serv_type == item.serv_type &&
                        x.truck_size == item.truck_size
                    );

                    if (existing != null)
                    {
                        // Update manual semua field (tanpa menyentuh id_seq)
                        existing.charge_uom = item.charge_uom;
                        existing.flag_min = item.flag_min.HasValue ? (byte?)item.flag_min : null;
                        existing.charge_min = item.charge_min;
                        existing.flag_range = item.flag_range.HasValue ? (byte?)item.flag_range : null;
                        existing.min_range = item.min_range;
                        existing.max_range = item.max_range;
                        existing.buy1 = item.buy1;
                        existing.buy2 = item.buy2;
                        existing.buy3 = item.buy3;
                        existing.buy_ret_empt = item.buy_ret_empt;
                        existing.buy_ret_cargo = item.buy_ret_cargo;
                        existing.buy_ovnight = item.buy_ovnight;
                        existing.buy_cancel = item.buy_cancel;
                        existing.buytrip2 = item.buytrip2;
                        existing.buytrip3 = item.buytrip3;
                        existing.buy_diff_area = item.buy_diff_area;
                        existing.valid_date = item.valid_date;
                        existing.active_flag = item.active_flag;
                        existing.curr = item.curr;
                        existing.rate_value = item.rate_value;
                        existing.entry_user = HttpContext.Session.GetString("username") ?? "System";
                        existing.entry_date = DateTime.Now;

                        continue; // Skip insert, karena sudah update
                    }
                }

                // Tambah baru
                _context.PriceBuys.Add(entity);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Data berhasil diproses." });
        }

    }
}
