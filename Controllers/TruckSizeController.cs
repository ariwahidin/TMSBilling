using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;


[SessionAuthorize]
public class TruckSizeController : Controller
{
    private readonly AppDbContext _context;

    public TruckSizeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var list = _context.TruckSizes.ToList();
        return View(list);
    }

    public IActionResult Form(int? id)
    {
        if (id == null)
        {
            return PartialView("_Form", new TruckSize
            {
                trucksize_code = string.Empty
            });
        }

        var data = _context.TruckSizes.FirstOrDefault(t => t.ID == id);
        return PartialView("_Form", data);
    }

    [HttpPost]
    public IActionResult Form(TruckSize model)
    {
        if (!ModelState.IsValid)
            return BadRequest();


        var existing = _context.TruckSizes.FirstOrDefault(t => t.ID == model.ID);

        if (existing == null)
        {
            bool exists = _context.TruckSizes.Any(v => v.trucksize_code == model.trucksize_code);
            if (exists)
            {
                return BadRequest(new
                {
                    message = "Truck Size ID already exists"
                });
            }

            model.entryuser= HttpContext.Session.GetString("username") ?? "System";
            model.entrydate = DateTime.Now;
            _context.TruckSizes.Add(model);
        }
        else
        {
            bool duplicate = _context.TruckSizes.Any(v => v.trucksize_code == model.trucksize_code && v.ID != model.ID);
            if (duplicate)
            {
                return BadRequest(new
                {
                    message = "Truck Size ID already exists on another record"
                });
            }

            existing.trucksize_code = model.trucksize_code;
            existing.updateuser = HttpContext.Session.GetString("username") ?? "System";
            existing.updatedate = DateTime.Now;
        }

        _context.SaveChanges();
        return Ok();
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.TruckSizes.FirstOrDefault(t => t.ID == id);
        if (data == null) return NotFound();

        _context.TruckSizes.Remove(data);
        _context.SaveChanges();

        return Ok();
    }



    [HttpPost]
    public IActionResult Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "File tidak ditemukan atau kosong." });
        }

        var added = new List<string>();
        var skippedExisting = new List<string>();
        var skippedDuplicateInFile = new List<string>();
        var invalid = new List<string>();

        // Load existing codes (case-insensitive) sekali di awal
        var existingCodes = _context.TruckSizes
            .Select(t => t.trucksize_code)
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
                return BadRequest(new { message = "File Excel kosong atau format tidak sesuai." });
            }

            foreach (var row in rows)
            {
                var rawCode = row.Cell(1).GetValue<string>()?.Trim();

                if (string.IsNullOrWhiteSpace(rawCode))
                    continue; // baris kosong, skip diam-diam

                if (rawCode.Length > 25)
                {
                    invalid.Add($"{rawCode} (lebih dari 25 karakter)");
                    continue;
                }

                var normalized = rawCode.ToUpperInvariant();

                if (existingCodes.Contains(normalized))
                {
                    skippedExisting.Add(rawCode);
                    continue;
                }

                if (seenInFile.Contains(normalized))
                {
                    skippedDuplicateInFile.Add(rawCode);
                    continue;
                }

                seenInFile.Add(normalized);

                _context.TruckSizes.Add(new TruckSize
                {
                    trucksize_code = rawCode,
                    entryuser = username,
                    entrydate = now
                });

                added.Add(rawCode);
            }

            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Gagal membaca file Excel: " + ex.Message });
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
}
