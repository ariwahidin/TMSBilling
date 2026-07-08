using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class OriginController : Controller
{
    private readonly AppDbContext _context;

    public OriginController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Menampilkan halaman daftar semua data Origin (asal pengiriman).
    /// Mengambil seluruh data dari tabel Origins dan menampilkan dalam view tabel.
    /// </summary>
    public IActionResult Index()
    {
        var list = _context.Origins.ToList();
        return View(list);
    }

    /// <summary>
    /// Menampilkan form partial view untuk tambah Origin baru atau edit Origin yang sudah ada.
    /// Jika id = null → form kosong untuk data baru. Jika id ada → load data existing untuk diedit.
    /// </summary>
    public IActionResult Form(int? id)
    {
        if (id == null)
        {
            return PartialView("_Form", new Origin
            {
                origin_code = string.Empty
            });
        }

        var data = _context.Origins.FirstOrDefault(o => o.id == id);
        return PartialView("_Form", data);
    }

    /// <summary>
    /// Menyimpan data Origin baru (insert) atau mengupdate data Origin yang sudah ada.
    /// Melakukan validasi duplikasi origin_code sebelum insert/update.
    /// Mencatat user dan waktu entry (untuk data baru) atau update (untuk data existing).
    /// </summary>
    [HttpPost]
    public IActionResult Form(Origin model)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var existing = _context.Origins.FirstOrDefault(o => o.id == model.id);

        if (existing == null)
        {

            bool exists = _context.Origins.Any(v => v.origin_code == model.origin_code);
            if (exists)
            {
                return BadRequest(new
                {
                    message = "Origin already exists"
                });
            }

            model.entryuser = HttpContext.Session.GetString("username") ?? "System";
            model.entrydate = DateTime.Now;
            _context.Origins.Add(model);
        }
        else
        {

            bool duplicate = _context.Origins.Any(v => v.origin_code == model.origin_code && v.id != model.id);
            if (duplicate)
            {
                return BadRequest(new
                {
                    message = "Origin already exists on another record"
                });
            }

            existing.origin_code = model.origin_code;
            existing.origin_loccode = model.origin_loccode;
            existing.updateuser = HttpContext.Session.GetString("username") ?? "System";
            existing.updatedate = DateTime.Now;
        }

        _context.SaveChanges();
        return Ok();
    }

    /// <summary>
    /// Menghapus data Origin berdasarkan ID.
    /// Mengembalikan NotFound jika data tidak ditemukan, atau Ok jika berhasil dihapus.
    /// </summary>
    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.Origins.FirstOrDefault(o => o.id == id);
        if (data == null) return NotFound();

        _context.Origins.Remove(data);
        _context.SaveChanges();

        return Ok();
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

        var existingCodes = _context.Origins
            .Select(o => o.origin_code)
            .ToList()
            .Where(c => c != null)
            .Select(c => c!.Trim().ToUpperInvariant())
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
                var originCode = row.Cell(1).GetValue<string>()?.Trim();
                var locCode = row.Cell(2).GetValue<string>()?.Trim();

                if (string.IsNullOrWhiteSpace(originCode))
                    continue; // empty row, skip silently

                // Length validation
                var lengthErrors = new List<string>();
                if (originCode.Length > 50) lengthErrors.Add("Origin Name > 50 chars");
                if (locCode?.Length > 20) lengthErrors.Add("Location Code > 20 chars");

                if (lengthErrors.Any())
                {
                    invalid.Add($"{originCode} ({string.Join(", ", lengthErrors)})");
                    continue;
                }

                var normalized = originCode.ToUpperInvariant();

                if (existingCodes.Contains(normalized))
                {
                    skippedExisting.Add(originCode);
                    continue;
                }

                if (seenInFile.Contains(normalized))
                {
                    skippedDuplicateInFile.Add(originCode);
                    continue;
                }

                seenInFile.Add(normalized);

                _context.Origins.Add(new Origin
                {
                    origin_code = originCode,
                    origin_loccode = locCode,
                    entryuser = username,
                    entrydate = now
                });

                added.Add(originCode);
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
        var worksheet = workbook.Worksheets.Add("Origin Template");

        string[] headers = { "Origin Name", "Location Code" };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Example row
        worksheet.Cell(2, 1).Value = "Jakarta Warehouse";
        worksheet.Cell(2, 2).Value = "LOC001";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "OriginUploadTemplate.xlsx");
    }

}
