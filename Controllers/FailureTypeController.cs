using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class FailureTypeController : Controller
{
    private readonly AppDbContext _context;

    public FailureTypeController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Menampilkan halaman daftar semua data Failure Type.
    /// </summary>
    public IActionResult Index()
    {
        var list = _context.FailureTypes.ToList();
        return View(list);
    }

    /// <summary>
    /// Menampilkan form partial view untuk tambah atau edit Failure Type.
    /// </summary>
    public IActionResult Form(int? id)
    {
        if (id == null)
        {
            return PartialView("_Form", new FailureType
            {
                failure_type_code = string.Empty
            });
        }

        var data = _context.FailureTypes.FirstOrDefault(o => o.id == id);
        return PartialView("_Form", data);
    }

    /// <summary>
    /// Menyimpan data Failure Type baru atau mengupdate data yang sudah ada.
    /// </summary>
    [HttpPost]
    public IActionResult Form(FailureType model)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var existing = _context.FailureTypes.FirstOrDefault(o => o.id == model.id);

        if (existing == null)
        {
            bool exists = _context.FailureTypes.Any(v => v.failure_type_code == model.failure_type_code);
            if (exists)
            {
                return BadRequest(new
                {
                    message = "Failure Type already exists"
                });
            }

            model.entryuser = HttpContext.Session.GetString("username") ?? "System";
            model.entrydate = DateTime.Now;
            _context.FailureTypes.Add(model);
        }
        else
        {
            bool duplicate = _context.FailureTypes.Any(v => v.failure_type_code == model.failure_type_code && v.id != model.id);
            if (duplicate)
            {
                return BadRequest(new
                {
                    message = "Failure Type already exists on another record"
                });
            }

            existing.failure_type_code = model.failure_type_code;
            existing.failure_type_description = model.failure_type_description;
            existing.updateuser = HttpContext.Session.GetString("username") ?? "System";
            existing.updatedate = DateTime.Now;
        }

        _context.SaveChanges();
        return Ok();
    }

    /// <summary>
    /// Menghapus data Failure Type berdasarkan ID.
    /// Akan ditolak jika masih ada Failure yang menggunakan type ini.
    /// </summary>
    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.FailureTypes.FirstOrDefault(o => o.id == id);
        if (data == null) return NotFound();

        bool isUsed = _context.Failures.Any(f => f.failure_type_id == id);
        if (isUsed)
        {
            return BadRequest(new
            {
                message = "Failure Type tidak dapat dihapus karena masih digunakan oleh data Failure."
            });
        }

        _context.FailureTypes.Remove(data);
        _context.SaveChanges();

        return Ok();
    }

    /// <summary>
    /// Upload data Failure Type dari file Excel.
    /// </summary>
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

        var existingCodes = _context.FailureTypes
            .Select(o => o.failure_type_code)
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
            var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1);

            if (rows == null)
            {
                return BadRequest(new { message = "Excel file is empty or format is invalid." });
            }

            foreach (var row in rows)
            {
                var typeCode = row.Cell(1).GetValue<string>()?.Trim();
                var typeDesc = row.Cell(2).GetValue<string>()?.Trim();

                if (string.IsNullOrWhiteSpace(typeCode))
                    continue;

                var lengthErrors = new List<string>();
                if (typeCode.Length > 50) lengthErrors.Add("Failure Type Code > 50 chars");
                if (typeDesc?.Length > 200) lengthErrors.Add("Description > 200 chars");

                if (lengthErrors.Any())
                {
                    invalid.Add($"{typeCode} ({string.Join(", ", lengthErrors)})");
                    continue;
                }

                var normalized = typeCode.ToUpperInvariant();

                if (existingCodes.Contains(normalized))
                {
                    skippedExisting.Add(typeCode);
                    continue;
                }

                if (seenInFile.Contains(normalized))
                {
                    skippedDuplicateInFile.Add(typeCode);
                    continue;
                }

                seenInFile.Add(normalized);

                _context.FailureTypes.Add(new FailureType
                {
                    failure_type_code = typeCode,
                    failure_type_description = typeDesc,
                    entryuser = username,
                    entrydate = now
                });

                added.Add(typeCode);
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

    /// <summary>
    /// Download template Excel untuk upload Failure Type.
    /// </summary>
    public IActionResult DownloadTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("FailureType Template");

        string[] headers = { "Failure Type Code", "Description" };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        worksheet.Cell(2, 1).Value = "ELECTRICAL";
        worksheet.Cell(2, 2).Value = "Electrical failure type";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "FailureTypeUploadTemplate.xlsx");
    }
}
