using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Models.UploadModels;

[SessionAuthorize]
public class PriceSellController : Controller
{
    private readonly AppDbContext _context;
    private readonly SelectListService _selectList;

    public PriceSellController(AppDbContext context, SelectListService selectList)
    {
        _context = context;
        _selectList = selectList;
    }

    public IActionResult Index()
    {
        return View(_context.PriceSells.ToList());
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

    [HttpPost]
    public IActionResult Save(PriceSell model)
    {
        if (!ModelState.IsValid)
            return PartialView("_Form", model);

        var username = HttpContext.Session.GetString("username") ?? "System";

        if (model.id_seq == 0)
        {
            model.entry_date = DateTime.Now;
            model.entry_user = username;
            _context.PriceSells.Add(model);
        }
        else
        {
            var db = _context.PriceSells.Find(model.id_seq);
            if (db == null) return NotFound();

            db.cust_code = model.cust_code;
            db.origin = model.origin;
            db.dest = model.dest;
            db.serv_type = model.serv_type;
            db.serv_moda = model.serv_moda;
            db.truck_size = model.truck_size;
            db.charge_uom = model.charge_uom;
            db.flag_min = model.flag_min;
            db.charge_min = model.charge_min;
            db.flag_range = model.flag_range;
            db.min_range = model.min_range;
            db.max_range = model.max_range;
            db.sell1 = model.sell1;
            db.sell2 = model.sell2;
            db.sell3 = model.sell3;
            db.sell_ret_empty = model.sell_ret_empty;
            db.sell_ret_cargo = model.sell_ret_cargo;
            db.sell_ovnight = model.sell_ovnight;
            db.sell_cancel = model.sell_cancel;
            db.selltrip2 = model.selltrip2;
            db.selltrip3 = model.selltrip3;
            db.sell_diff_area = model.sell_diff_area;
            db.valid_date = model.valid_date;
            db.curr = model.curr;
            db.rate_value = model.rate_value;
            db.active_flag = model.active_flag;
            db.update_user = username;
            db.update_date = DateTime.Now;
        }

        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var model = _context.PriceSells.Find(id);
        if (model == null) return NotFound();

        _context.PriceSells.Remove(model);
        _context.SaveChanges();
        return Ok();
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

        // 2. Sheet Master
        //CreateMasterSheet(workbook, "CustCode",
        //    _context.Customers.Select(c => c.CUST_CODE).Distinct().ToArray());

        //CreateMasterSheet(workbook, "Origin",
        //    _context.Origins.Select(o => o.origin_code).Distinct().ToArray());

        //CreateMasterSheet(workbook, "Dest",
        //    _context.Destinations.Select(d => d.destination_code).Distinct().ToArray());

        //CreateMasterSheet(workbook, "ServType",
        //    _context.ServiceTypes.Select(s => s.serv_name).Distinct().ToArray());

        //CreateMasterSheet(workbook, "ServModa",
        //    _context.ServiceModas.Select(s => s.moda_name).Distinct().ToArray());

        //CreateMasterSheet(workbook, "TruckSize",
        //    _context.TruckSizes.Select(t => t.trucksize_code).Distinct().ToArray());

        //CreateMasterSheet(workbook, "ChargeUom",
        //    _context.ChargeUoms.Select(u => u.charge_name).Distinct().ToArray());

        // 3. Output
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Template_Upload_PriceSell.xlsx");
    }

    // Fungsi bantu tetap sama
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
            return (false, $"Kode customer main tidak valid: {item.cust_code}");

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
    public async Task<IActionResult> UploadExcel([FromBody] UploadPriceSellRequest request)
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

        // VALIDASI DUPLIKAT DI FILE
        var duplicates = request.data
            .GroupBy(x => new { x.cust_code, x.origin, x.dest, x.serv_type, x.serv_moda, x.truck_size, x.charge_uom })
            .Where(g => g.Count() > 1)
            .Select(g => $"Data duplikat ditemukan: {g.Key.cust_code} | {g.Key.origin} | {g.Key.dest} | {g.Key.serv_type} | {g.Key.serv_moda} | {g.Key.truck_size} | {g.Key.charge_uom}")
            .ToList();

        if (duplicates.Any())
            return BadRequest(new { message = "Duplikat data ditemukan dalam file upload.", errors = duplicates });

        // VALIDASI MODE
        var modeErrors = new List<string>();

        foreach (var item in request.data)
        { 
            var query = _context.PriceSells.Where(x =>
                x.cust_code == item.cust_code &&
                x.origin == item.origin &&
                x.dest == item.dest &&
                x.serv_type == item.serv_type &&
                x.serv_moda == item.serv_moda &&
                x.truck_size == item.truck_size &&
                x.charge_uom == item.charge_uom
            );

            var exists = await query.AnyAsync();

            if (request.mode == "add" && exists)
                modeErrors.Add($"(ADD) Data sudah ada: {item.cust_code} | {item.origin} | {item.dest} | {item.serv_type} | {item.serv_moda} | {item.truck_size} | {item.charge_uom}");

            if (request.mode == "edit" && !exists)
                modeErrors.Add($"(EDIT) Data tidak ditemukan: {item.cust_code} | {item.origin} | {item.dest} | {item.serv_type} | {item.serv_moda} | {item.truck_size} | {item.charge_uom}");
        }

        if (modeErrors.Any())
            return BadRequest(new { message = "Validasi berdasarkan mode gagal.", errors = modeErrors });

        // VALIDASI MASTER
        var validationErrors = new List<string>();
        foreach (var item in request.data)
        {
            var (isValid, errorMessage) = await ValidatePriceSellItemAsync(item);
            if (!isValid && errorMessage != null)
                validationErrors.Add($"[{item.cust_code}-{item.origin}-{item.dest}] {errorMessage}");
        }

        if (validationErrors.Any())
            return BadRequest(new { message = "Validasi master gagal.", errors = validationErrors });

        // INSERT / UPDATE
        foreach (var item in request.data)
        {
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
                entry_user = HttpContext.Session.GetString("username") ?? "System",
                entry_date = DateTime.Now
            };

            if (request.mode == "edit")
            {
                var existing = await _context.PriceSells.FirstOrDefaultAsync(x =>
                    x.cust_code == item.cust_code &&
                    x.origin == item.origin &&
                    x.dest == item.dest &&
                    x.serv_type == item.serv_type &&
                    x.serv_moda == item.serv_moda &&
                    x.truck_size == item.truck_size &&
                    x.charge_uom == item.charge_uom
                );

                if (existing != null)
                {
                    // Update all fields
                    existing.flag_min = entity.flag_min;
                    existing.charge_min = entity.charge_min;
                    existing.flag_range = entity.flag_range;
                    existing.min_range = entity.min_range;
                    existing.max_range = entity.max_range;
                    existing.sell1 = entity.sell1;
                    existing.sell2 = entity.sell2;
                    existing.sell3 = entity.sell3;
                    existing.sell_ret_empty = entity.sell_ret_empty;
                    existing.sell_ret_cargo = entity.sell_ret_cargo;
                    existing.sell_ovnight = entity.sell_ovnight;
                    existing.sell_cancel = entity.sell_cancel;
                    existing.selltrip2 = entity.selltrip2;
                    existing.selltrip3 = entity.selltrip3;
                    existing.sell_diff_area = entity.sell_diff_area;
                    existing.valid_date = entity.valid_date;
                    existing.active_flag = entity.active_flag;
                    existing.curr = entity.curr;
                    existing.rate_value = entity.rate_value;
                    existing.update_user = HttpContext.Session.GetString("username") ?? "System";
                    existing.update_date = DateTime.Now;

                    continue; // skip insert
                }
            }

            // Tambah baru
            _context.PriceSells.Add(entity);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Data PriceSell berhasil diproses." });
    }


}
