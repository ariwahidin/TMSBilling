using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class DestinationController : Controller
{
    private readonly AppDbContext _context;
    private readonly SelectListService _selectList;

    public DestinationController(AppDbContext context, SelectListService selectList)
    {
        _context = context;
        _selectList = selectList;
    }

    public IActionResult Index()
    {
        var username = HttpContext.Session.GetString("username") ?? "System";
        var accessibleCustomers = _context.UserXCustomers
        .Where(x => x.UserName == username)
        .Select(x => x.CustomerMain)
        .Distinct()
        .ToList();

        var list = _context.Destinations
            .Where(d => accessibleCustomers.Contains(d.MAIN_CUST))
            .ToList();
        return View(list);
    }

    public IActionResult Form(int? id)
    {
        ViewBag.ListArea = _selectList.getArea();
        ViewBag.ListCustomer = _context.CustomerMains
            .OrderBy(c => c.MAIN_CUST)
            .Select(c => new SelectListItem
            {
                Value = c.MAIN_CUST,
                Text = c.MAIN_CUST
            }).ToList();

        if (id == null)
        {
            return PartialView("_Form", new Destination
            {
                destination_code = string.Empty
            });
        }

        var data = _context.Destinations.FirstOrDefault(d => d.ID == id);
        return PartialView("_Form", data);
    }

    [HttpPost]
    public IActionResult Form(Destination model)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var existing = _context.Destinations.FirstOrDefault(d => d.ID == model.ID);
        if (existing == null)
        {
            bool exists = _context.Destinations.Any(v =>
                v.destination_code == model.destination_code &&
                v.MAIN_CUST == model.MAIN_CUST);
            if (exists)
                return BadRequest(new { message = "Destination sudah ada untuk customer ini" });

            model.entryuser = HttpContext.Session.GetString("username") ?? "System";
            model.entrydate = DateTime.Now;
            _context.Destinations.Add(model);
        }
        else
        {
            bool duplicate = _context.Destinations.Any(v =>
                v.destination_code == model.destination_code &&
                v.MAIN_CUST == model.MAIN_CUST &&
                v.ID != model.ID);
            if (duplicate)
                return BadRequest(new { message = "Destination sudah ada pada record lain" });

            existing.destination_code = model.destination_code;
            existing.dest_loccode = model.dest_loccode;
            existing.area = model.area;
            existing.MAIN_CUST = model.MAIN_CUST;
            existing.updateuser = HttpContext.Session.GetString("username") ?? "System";
            existing.updatedate = DateTime.Now;
        }

        _context.SaveChanges();
        return Ok();
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.Destinations.FirstOrDefault(d => d.ID == id);
        if (data == null) return NotFound();
        _context.Destinations.Remove(data);
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

        var username = HttpContext.Session.GetString("username") ?? "System";

        // Customer yang boleh diakses user ini
        var accessibleCustomers = _context.UserXCustomers
            .Where(x => x.UserName == username)
            .Select(x => x.CustomerMain)
            .Distinct()
            .ToList()
            .Select(c => c.Trim().ToUpperInvariant())
            .ToHashSet();

        var added = new List<string>();
        var skippedExisting = new List<string>();
        var skippedCustomerNotFound = new List<string>();
        var skippedCustomerNoAccess = new List<string>();
        var skippedDuplicateInFile = new List<string>();
        var invalid = new List<string>();

        // Master validasi
        var validCustomers = _context.CustomerMains
            .Select(c => c.MAIN_CUST)
            .ToList()
            .Select(c => c.Trim().ToUpperInvariant())
            .ToHashSet();

        // Reuse list area yang sama dengan form manual (source of truth sama)
        var validAreas = _selectList.getArea()
            .Select(a => a.Value?.Trim().ToUpperInvariant())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .ToHashSet();

        // Existing composite key: destination_code + MAIN_CUST
        var existingDestinations = _context.Destinations
            .Select(d => new { d.destination_code, d.MAIN_CUST })
            .ToList()
            .Where(d => d.destination_code != null && d.MAIN_CUST != null)
            .Select(d => $"{d.destination_code!.Trim().ToUpperInvariant()}|{d.MAIN_CUST!.Trim().ToUpperInvariant()}")
            .ToHashSet();

        var seenInFile = new HashSet<string>();
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
                var custMain = row.Cell(1).GetValue<string>()?.Trim();
                var destCode = row.Cell(2).GetValue<string>()?.Trim();
                var locCode = row.Cell(3).GetValue<string>()?.Trim();
                var area = row.Cell(4).GetValue<string>()?.Trim();

                if (string.IsNullOrWhiteSpace(custMain) && string.IsNullOrWhiteSpace(destCode))
                    continue; // empty row, skip silently

                var identifier = !string.IsNullOrWhiteSpace(destCode) ? destCode : "(no destination name)";

                // Required fields
                var requiredErrors = new List<string>();
                if (string.IsNullOrWhiteSpace(custMain)) requiredErrors.Add("Customer is required");
                if (string.IsNullOrWhiteSpace(destCode)) requiredErrors.Add("Destination Name is required");

                if (requiredErrors.Any())
                {
                    invalid.Add($"{identifier} ({string.Join(", ", requiredErrors)})");
                    continue;
                }

                // Length validation
                var lengthErrors = new List<string>();
                if (destCode!.Length > 100) lengthErrors.Add("Destination Name > 100 chars");
                if (locCode?.Length > 100) lengthErrors.Add("Location Code > 100 chars");
                if (area?.Length > 20) lengthErrors.Add("Area > 20 chars");
                if (custMain!.Length > 30) lengthErrors.Add("Customer > 30 chars");

                if (lengthErrors.Any())
                {
                    invalid.Add($"{identifier} ({string.Join(", ", lengthErrors)})");
                    continue;
                }

                var normalizedCust = custMain.ToUpperInvariant();

                // Master validation — skip if not found
                if (!validCustomers.Contains(normalizedCust))
                {
                    skippedCustomerNotFound.Add($"{destCode} (Customer '{custMain}' not found)");
                    continue;
                }

                // Access validation — skip if user has no access to this customer
                if (!accessibleCustomers.Contains(normalizedCust))
                {
                    skippedCustomerNoAccess.Add($"{destCode} (No access to customer '{custMain}')");
                    continue;
                }

                // Area validation — hanya jika diisi
                if (!string.IsNullOrWhiteSpace(area) && !validAreas.Contains(area.ToUpperInvariant()))
                {
                    invalid.Add($"{destCode} (Area '{area}' is not a valid option)");
                    continue;
                }

                // Composite key check
                var compositeKey = $"{destCode.ToUpperInvariant()}|{normalizedCust}";

                if (existingDestinations.Contains(compositeKey))
                {
                    skippedExisting.Add($"{destCode} (Customer: {custMain})");
                    continue;
                }

                if (seenInFile.Contains(compositeKey))
                {
                    skippedDuplicateInFile.Add($"{destCode} (Customer: {custMain})");
                    continue;
                }

                seenInFile.Add(compositeKey);

                _context.Destinations.Add(new Destination
                {
                    destination_code = destCode,
                    dest_loccode = locCode,
                    area = area,
                    MAIN_CUST = custMain,
                    entryuser = username,
                    entrydate = now
                });

                added.Add($"{destCode} (Customer: {custMain})");
            }

            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to read Excel file: " + ex.Message });
        }

        return Ok(new
        {
            totalProcessed = added.Count + skippedExisting.Count + skippedCustomerNotFound.Count
                + skippedCustomerNoAccess.Count + skippedDuplicateInFile.Count + invalid.Count,
            added,
            skippedExisting,
            skippedCustomerNotFound,
            skippedCustomerNoAccess,
            skippedDuplicateInFile,
            invalid
        });
    }

    public IActionResult DownloadTemplate()
    {
        var username = HttpContext.Session.GetString("username") ?? "System";

        var accessibleCustomers = _context.UserXCustomers
            .Where(x => x.UserName == username)
            .Select(x => x.CustomerMain)
            .Distinct()
            .ToList();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Destination Template");

        string[] headers = { "Customer", "Destination Name", "Location Code", "Area" };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Example row
        if (accessibleCustomers.Any())
            worksheet.Cell(2, 1).Value = accessibleCustomers.First();
        worksheet.Cell(2, 2).Value = "Warehouse Jakarta";
        worksheet.Cell(2, 3).Value = "LOC001";
        worksheet.Cell(2, 4).Value = "";

        worksheet.Columns().AdjustToContents();

        // Reference sheet — hanya customer yang bisa diakses user ini + area list
        var refSheet = workbook.Worksheets.Add("Reference Lists");
        refSheet.Cell(1, 1).Value = "Your Accessible Customers";
        refSheet.Cell(1, 2).Value = "Valid Area (optional)";
        refSheet.Range(1, 1, 1, 2).Style.Font.Bold = true;

        for (int i = 0; i < accessibleCustomers.Count; i++)
            refSheet.Cell(i + 2, 1).Value = accessibleCustomers[i];

        var areaList = _selectList.getArea().Select(a => a.Value).ToList();
        for (int i = 0; i < areaList.Count; i++)
            refSheet.Cell(i + 2, 2).Value = areaList[i];

        refSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "DestinationUploadTemplate.xlsx");
    }
}