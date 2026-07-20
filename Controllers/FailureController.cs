using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class FailureController : Controller
{
    private readonly AppDbContext _context;

    public FailureController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Menampilkan halaman daftar semua data Failure dengan informasi Failure Type.
    /// </summary>
    public IActionResult Index()
    {
        var list = _context.Failures.Include(f => f.FailureType).ToList();
        return View(list);
    }

    /// <summary>
    /// Menampilkan form partial view untuk tambah atau edit Failure.
    /// Load dropdown Failure Type via ViewBag.
    /// </summary>
    public IActionResult Form(int? id)
    {
        ViewBag.FailureTypes = _context.FailureTypes.OrderBy(ft => ft.failure_type_code).ToList();

        if (id == null)
        {
            return PartialView("_Form", new Failure
            {
                failure_code = string.Empty
            });
        }

        var data = _context.Failures.FirstOrDefault(o => o.id == id);
        return PartialView("_Form", data);
    }

    /// <summary>
    /// Menyimpan data Failure baru atau mengupdate data yang sudah ada.
    /// </summary>
    [HttpPost]
    public IActionResult Form(Failure model)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var existing = _context.Failures.FirstOrDefault(o => o.id == model.id);

        if (existing == null)
        {
            bool exists = _context.Failures.Any(v => v.failure_code == model.failure_code);
            if (exists)
            {
                return BadRequest(new
                {
                    message = "Failure already exists"
                });
            }

            model.entryuser = HttpContext.Session.GetString("username") ?? "System";
            model.entrydate = DateTime.Now;
            _context.Failures.Add(model);
        }
        else
        {
            bool duplicate = _context.Failures.Any(v => v.failure_code == model.failure_code && v.id != model.id);
            if (duplicate)
            {
                return BadRequest(new
                {
                    message = "Failure already exists on another record"
                });
            }

            existing.failure_code = model.failure_code;
            existing.failure_name = model.failure_name;
            existing.failure_description = model.failure_description;
            existing.failure_type_id = model.failure_type_id;
            existing.updateuser = HttpContext.Session.GetString("username") ?? "System";
            existing.updatedate = DateTime.Now;
        }

        _context.SaveChanges();
        return Ok();
    }

    /// <summary>
    /// Menghapus data Failure berdasarkan ID.
    /// </summary>
    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.Failures.FirstOrDefault(o => o.id == id);
        if (data == null) return NotFound();

        _context.Failures.Remove(data);
        _context.SaveChanges();

        return Ok();
    }

    /// <summary>
    /// Upload data Failure dari file Excel.
    /// Kolom Failure Type Code akan di-resolve ke failure_type_id.
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

        var existingCodes = _context.Failures
            .Select(o => o.failure_code)
            .ToList()
            .Where(c => c != null)
            .Select(c => c!.Trim().ToUpperInvariant())
            .ToHashSet();

        // Build lookup: failure_type_code (uppercase) → id
        var failureTypeLookup = _context.FailureTypes
            .Where(ft => ft.failure_type_code != null)
            .ToList()
            .ToDictionary(
                ft => ft.failure_type_code!.Trim().ToUpperInvariant(),
                ft => ft.id
            );

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
                var failureCode = row.Cell(1).GetValue<string>()?.Trim();
                var failureName = row.Cell(2).GetValue<string>()?.Trim();
                var failureDesc = row.Cell(3).GetValue<string>()?.Trim();
                var failureTypeCode = row.Cell(4).GetValue<string>()?.Trim();

                if (string.IsNullOrWhiteSpace(failureCode))
                    continue;

                var lengthErrors = new List<string>();
                if (failureCode.Length > 50) lengthErrors.Add("Failure Code > 50 chars");
                if (failureName?.Length > 100) lengthErrors.Add("Failure Name > 100 chars");
                if (failureDesc?.Length > 200) lengthErrors.Add("Description > 200 chars");

                // Resolve failure type
                int? resolvedTypeId = null;
                if (!string.IsNullOrWhiteSpace(failureTypeCode))
                {
                    var normalizedType = failureTypeCode.ToUpperInvariant();
                    if (!failureTypeLookup.TryGetValue(normalizedType, out int typeId))
                    {
                        lengthErrors.Add($"Failure Type '{failureTypeCode}' not found");
                    }
                    else
                    {
                        resolvedTypeId = typeId;
                    }
                }

                if (lengthErrors.Any())
                {
                    invalid.Add($"{failureCode} ({string.Join(", ", lengthErrors)})");
                    continue;
                }

                var normalized = failureCode.ToUpperInvariant();

                if (existingCodes.Contains(normalized))
                {
                    skippedExisting.Add(failureCode);
                    continue;
                }

                if (seenInFile.Contains(normalized))
                {
                    skippedDuplicateInFile.Add(failureCode);
                    continue;
                }

                seenInFile.Add(normalized);

                _context.Failures.Add(new Failure
                {
                    failure_code = failureCode,
                    failure_name = failureName,
                    failure_description = failureDesc,
                    failure_type_id = resolvedTypeId,
                    entryuser = username,
                    entrydate = now
                });

                added.Add(failureCode);
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
    /// Download template Excel untuk upload Failure.
    /// </summary>
    public IActionResult DownloadTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Failure Template");

        string[] headers = { "Failure Code", "Failure Name", "Description", "Failure Type Code" };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        worksheet.Cell(2, 1).Value = "ENG001";
        worksheet.Cell(2, 2).Value = "Engine Overheat";
        worksheet.Cell(2, 3).Value = "Engine temperature exceeds limit";
        worksheet.Cell(2, 4).Value = "ELECTRICAL";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "FailureUploadTemplate.xlsx");
    }
}
