using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class VendorController : Controller
{
    private readonly AppDbContext _context;

    public VendorController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var vendors = _context.Vendors.ToList();
        return View(vendors);
    }

    public IActionResult Create()
    {
        return View("Form", new Vendor
        {
            SUP_CODE = string.Empty // atau nilai default sesuai kebutuhan
        });
    }


    [HttpPost]
    public IActionResult Create(Vendor model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        // Cek apakah SUP_CODE sudah ada
        bool exists = _context.Vendors.Any(v => v.SUP_CODE == model.SUP_CODE);
        if (exists)
        {
            ModelState.AddModelError("SUP_CODE", "Vendor ID already exists");
            return View("Form", model);
        }

        model.ENTRY_USER = HttpContext.Session.GetString("username") ?? "System";
        model.ENTRY_DATE = DateTime.Now;
        _context.Vendors.Add(model);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    public IActionResult Edit(int id)
    {
        var data = _context.Vendors.FirstOrDefault(v => v.ID == id);
        if (data == null) return NotFound();
        return View("Form", data);
    }


    [HttpPost]
    public IActionResult Edit(Vendor model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var existing = _context.Vendors.FirstOrDefault(v => v.ID == model.ID);
        if (existing == null) return NotFound();

        // Cek apakah SUP_CODE digunakan oleh vendor lain
        bool duplicate = _context.Vendors.Any(v => v.SUP_CODE == model.SUP_CODE && v.ID != model.ID);
        if (duplicate)
        {
            ModelState.AddModelError("SUP_CODE", "Vendor ID already exists on another record");
            return View("Form", model);
        }

        existing.SUP_CODE = model.SUP_CODE;
        existing.SUP_TYPE = model.SUP_TYPE;
        existing.SUP_NAME = model.SUP_NAME;
        existing.SUP_ADDR1 = model.SUP_ADDR1;
        existing.SUP_ADDR2 = model.SUP_ADDR2;
        existing.SUP_CITY = model.SUP_CITY;
        existing.SUP_EMAIL = model.SUP_EMAIL;
        existing.SUP_TEL = model.SUP_TEL;
        existing.SUP_FAX = model.SUP_FAX;
        existing.SUP_PIC = model.SUP_PIC;
        existing.TAX_REG_NO = model.TAX_REG_NO;
        existing.ACTIVE_FLAG = model.ACTIVE_FLAG;
        existing.UPDATE_USER = HttpContext.Session.GetString("username") ?? "System";
        existing.UPDATE_DATE = DateTime.Now;

        _context.SaveChanges();
        return RedirectToAction("Index");
    }


    [HttpPost]
    public IActionResult Delete(int id)
    {
        var vendor = _context.Vendors.FirstOrDefault(v => v.ID == id);
        if (vendor == null) return NotFound();

        _context.Vendors.Remove(vendor);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }


    [HttpPost]
    public IActionResult Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "File not found or empty." });
        }

        var added = new List<string>();
        var skippedExisting = new List<string>();
        var skippedDuplicateInFile = new List<string>();
        var invalid = new List<string>();

        var existingCodes = _context.Vendors
            .Select(v => v.SUP_CODE)
            .ToList()
            .Select(c => c.Trim().ToUpperInvariant())
            .ToHashSet();

        var seenInFile = new HashSet<string>();
        var username = HttpContext.Session.GetString("username") ?? "System";
        var now = DateTime.Now;

        try
        {
            using var stream = new MemoryStream();
            file.CopyTo(stream);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1); // skip header

            if (rows == null)
            {
                return BadRequest(new { message = "Excel file is empty or format is invalid." });
            }

            foreach (var row in rows)
            {
                var code = row.Cell(1).GetValue<string>()?.Trim();
                var type = row.Cell(2).GetValue<string>()?.Trim();
                var name = row.Cell(3).GetValue<string>()?.Trim();
                var addr1 = row.Cell(4).GetValue<string>()?.Trim();
                var addr2 = row.Cell(5).GetValue<string>()?.Trim();
                var city = row.Cell(6).GetValue<string>()?.Trim();
                var email = row.Cell(7).GetValue<string>()?.Trim();
                var phone = row.Cell(8).GetValue<string>()?.Trim();
                var fax = row.Cell(9).GetValue<string>()?.Trim();
                var pic = row.Cell(10).GetValue<string>()?.Trim();
                var taxRegNo = row.Cell(11).GetValue<string>()?.Trim();

                if (string.IsNullOrWhiteSpace(code))
                    continue; // empty row, skip silently

                // Length validation
                var lengthErrors = new List<string>();
                if (code.Length > 50) lengthErrors.Add("Vendor Code > 50 chars");
                if (type?.Length > 10) lengthErrors.Add("Vendor Type > 10 chars");
                if (name?.Length > 50) lengthErrors.Add("Vendor Name > 50 chars");
                if (addr1?.Length > 50) lengthErrors.Add("Address 1 > 50 chars");
                if (addr2?.Length > 50) lengthErrors.Add("Address 2 > 50 chars");
                if (city?.Length > 50) lengthErrors.Add("City > 50 chars");
                if (email?.Length > 50) lengthErrors.Add("Email > 50 chars");
                if (phone?.Length > 20) lengthErrors.Add("Phone > 20 chars");
                if (fax?.Length > 20) lengthErrors.Add("Fax > 20 chars");
                if (pic?.Length > 50) lengthErrors.Add("PIC > 50 chars");
                if (taxRegNo?.Length > 25) lengthErrors.Add("Tax Reg No > 25 chars");

                if (lengthErrors.Any())
                {
                    invalid.Add($"{code} ({string.Join(", ", lengthErrors)})");
                    continue;
                }

                var normalized = code.ToUpperInvariant();

                if (existingCodes.Contains(normalized))
                {
                    skippedExisting.Add(code);
                    continue;
                }

                if (seenInFile.Contains(normalized))
                {
                    skippedDuplicateInFile.Add(code);
                    continue;
                }

                seenInFile.Add(normalized);

                _context.Vendors.Add(new Vendor
                {
                    SUP_CODE = code,
                    SUP_TYPE = type,
                    SUP_NAME = name,
                    SUP_ADDR1 = addr1,
                    SUP_ADDR2 = addr2,
                    SUP_CITY = city,
                    SUP_EMAIL = email,
                    SUP_TEL = phone,
                    SUP_FAX = fax,
                    SUP_PIC = pic,
                    TAX_REG_NO = taxRegNo,
                    ACTIVE_FLAG = 1,
                    ENTRY_USER = username,
                    ENTRY_DATE = now
                });

                added.Add(code);
            }

            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to read Excel file: " + ex.Message });
        }

        return Ok(new
        {
            totalProcessed = added.Count + skippedExisting.Count + skippedDuplicateInFile.Count + invalid.Count,
            added,
            skippedExisting,
            skippedDuplicateInFile,
            invalid
        });
    }

    public IActionResult DownloadTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Vendor Template");

        string[] headers = {
        "Vendor Code", "Vendor Type", "Vendor Name", "Address 1", "Address 2",
        "City", "Email", "Phone", "Fax", "PIC", "Tax Reg No"
    };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Example row (optional, helps user understand format)
        worksheet.Cell(2, 1).Value = "V001";
        worksheet.Cell(2, 2).Value = "Trucking";
        worksheet.Cell(2, 3).Value = "PT Example Logistics";
        worksheet.Cell(2, 6).Value = "Jakarta";
        worksheet.Cell(2, 7).Value = "info@example.com";
        worksheet.Cell(2, 8).Value = "021-1234567";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "VendorUploadTemplate.xlsx");
    }

}
