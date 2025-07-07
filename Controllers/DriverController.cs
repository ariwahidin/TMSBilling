using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class DriverController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public DriverController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public IActionResult Index()
    {
        var data = _context.Drivers.ToList();
        return View(data);
    }

    private string GenerateDriverId()
    {
        var lastDriver = _context.Drivers
            .OrderByDescending(d => d.ID)
            .FirstOrDefault();

        int lastNumber = 0;

        if (lastDriver != null && !string.IsNullOrEmpty(lastDriver.driver_id))
        {
            // Misalnya last driver_id = DR0023
            var numberPart = lastDriver.driver_id.Substring(2); // ambil "0023"
            int.TryParse(numberPart, out lastNumber);
        }

        // Naikkan nomor dan format jadi DR0001, DR0002, dst
        return "DR" + (lastNumber + 1).ToString("D4");
    }


    public IActionResult Create()
    {
        return View("Form", new Driver
        {
            driver_id = string.Empty,
            driver_name = string.Empty,
            vendor_id = string.Empty,
        });     
    }

    [HttpPost]
    public IActionResult Create(Driver model, IFormFile? photoFile)
    {

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                .Select(x => new {
                    Field = x.Key,
                    Errors = x.Value!.Errors.Select(e => e.ErrorMessage) // pakai ! karena udah difilter
                });

            foreach (var error in errors)
            {
                Console.WriteLine($"Field: {error.Field}, Error: {string.Join(", ", error.Errors)}");
            }

            return View("Form", model);
        }

        model.driver_id = GenerateDriverId();

        if (photoFile != null)
        {
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(photoFile.FileName)}";
            var path = Path.Combine(_env.WebRootPath, "uploads", "drivers");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var filePath = Path.Combine(path, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            photoFile.CopyTo(stream);

            model.driver_photo = "/uploads/drivers/" + fileName;
        }

        model.date_entry = DateTime.Now;
        model.user_entry = HttpContext.Session.GetString("username") ?? "System";

        _context.Drivers.Add(model);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    public IActionResult Edit(int id)
    {
        var driver = _context.Drivers.FirstOrDefault(x => x.ID == id);
        if (driver == null) return NotFound();

        return View("Form", driver);
    }

    [HttpPost]
    public IActionResult Edit(Driver model, IFormFile? photoFile)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var existing = _context.Drivers.FirstOrDefault(x => x.ID == model.ID);
        if (existing == null) return NotFound();

        if (photoFile != null)
        {
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(photoFile.FileName)}";
            var path = Path.Combine(_env.WebRootPath, "uploads", "drivers");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var filePath = Path.Combine(path, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            photoFile.CopyTo(stream);

            existing.driver_photo = "/uploads/drivers/" + fileName;
        }

        existing.driver_id = model.driver_id;
        existing.driver_name = model.driver_name;
        existing.vendor_id = model.vendor_id;
        existing.driver_birth = model.driver_birth;
        existing.driver_address = model.driver_address;
        existing.driver_sim = model.driver_sim;
        existing.driver_sim_exp = model.driver_sim_exp;
        existing.driver_nik = model.driver_nik;
        existing.driver_status = model.driver_status;
        existing.driver_remark = model.driver_remark;
        existing.driver_date_terminate = model.driver_date_terminate;
        existing.terminate_reason = model.terminate_reason;
        existing.vehicle_type = model.vehicle_type;

        existing.user_update = HttpContext.Session.GetString("username") ?? "System";
        existing.date_update = DateTime.Now;

        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var driver = _context.Drivers.FirstOrDefault(x => x.ID == id);
        if (driver == null) return NotFound();

        _context.Drivers.Remove(driver);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult ExportExcel()
    {
        var drivers = _context.Drivers.ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Drivers");

        // Header
        ws.Cell(1, 1).Value = "Driver ID";
        ws.Cell(1, 2).Value = "Name";
        ws.Cell(1, 3).Value = "Vendor";
        ws.Cell(1, 4).Value = "SIM";
        ws.Cell(1, 5).Value = "Status";

        // Data
        int row = 2;
        foreach (var d in drivers)
        {
            ws.Cell(row, 1).Value = d.driver_id;
            ws.Cell(row, 2).Value = d.driver_name;
            ws.Cell(row, 3).Value = d.vendor_id;
            ws.Cell(row, 4).Value = d.driver_sim;
            ws.Cell(row, 5).Value = d.driver_status == 1 ? "Aktif" : "Tidak Aktif";
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"DriverList_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
