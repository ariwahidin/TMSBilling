using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class LeadTimeController : Controller
{
    private readonly AppDbContext _context;

    public LeadTimeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var list = _context.LeadTimes.ToList();
        return View(list);
    }

    public IActionResult Form(int? id)
    {
        PopulateDropdowns();

        if (id == null)
        {
            return PartialView("_Form", new LeadTime());
        }

        var data = _context.LeadTimes.FirstOrDefault(o => o.id_seq == id);
        if (data == null) return NotFound();

        return PartialView("_Form", data);
    }

    [HttpPost]
    public IActionResult Form(LeadTime model)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var existing = _context.LeadTimes.FirstOrDefault(o => o.id_seq == model.id_seq);

        if (existing == null)
        {
            bool duplicate = IsDuplicate(model.cust_code, model.origin, model.dest, model.serv_type, model.serv_moda, model.truck_size);
            if (duplicate)
                return BadRequest(new { message = "Lead time for this combination already exists." });

            model.entry_user = HttpContext.Session.GetString("username") ?? "System";
            model.entry_date = DateTime.Now;
            model.active_flag = 1;
            _context.LeadTimes.Add(model);
        }
        else
        {
            bool duplicate = IsDuplicate(model.cust_code, model.origin, model.dest, model.serv_type, model.serv_moda, model.truck_size, model.id_seq);
            if (duplicate)
                return BadRequest(new { message = "Lead time for this combination already exists on another record." });

            existing.cust_code = model.cust_code;
            existing.origin = model.origin;
            existing.dest = model.dest;
            existing.serv_type = model.serv_type;
            existing.serv_moda = model.serv_moda;
            existing.truck_size = model.truck_size;
            existing.delivery_days = model.delivery_days;
            existing.pod_days = model.pod_days;
            existing.active_flag = model.active_flag;
            existing.update_user = HttpContext.Session.GetString("username") ?? "System";
            existing.update_date = DateTime.Now;
        }

        _context.SaveChanges();
        return Ok();
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.LeadTimes.FirstOrDefault(o => o.id_seq == id);
        if (data == null) return NotFound();

        _context.LeadTimes.Remove(data);
        _context.SaveChanges();
        return Ok();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private bool IsDuplicate(string? custCode, string? origin, string? dest,
        string? servType, string? servModa, string? truckSize, int? excludeId = null)
    {
        var query = _context.LeadTimes.Where(x =>
            x.cust_code == custCode &&
            x.origin == origin &&
            x.dest == dest &&
            x.serv_type == servType &&
            x.serv_moda == servModa &&
            x.truck_size == truckSize);

        if (excludeId.HasValue)
            query = query.Where(x => x.id_seq != excludeId.Value);

        return query.Any();
    }

    private void PopulateDropdowns()
    {
        ViewBag.Origins = _context.Origins.OrderBy(o => o.origin_code).ToList();
        ViewBag.Destinations = _context.Destinations.OrderBy(d => d.destination_code).ToList();
        ViewBag.Customers = _context.CustomerMains
                                       .Where(c => c.STATUS_FLAG == 1)
                                       .OrderBy(c => c.CUST_NAME)
                                       .ToList();
        ViewBag.ServiceTypes = _context.ServiceTypes.OrderBy(s => s.serv_name).ToList();
        ViewBag.ServiceModas = _context.ServiceModas.OrderBy(s => s.moda_name).ToList();
        ViewBag.TruckSizes = _context.TruckSizes.OrderBy(t => t.trucksize_code).ToList();
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
        var skippedCustomerNotFound = new List<string>();
        var skippedOriginNotFound = new List<string>();
        var skippedDestNotFound = new List<string>();
        var skippedServTypeNotFound = new List<string>();
        var skippedServModaNotFound = new List<string>();
        var skippedTruckSizeNotFound = new List<string>();
        var skippedDuplicateInFile = new List<string>();
        var invalid = new List<string>();

        // Master validation sets
        var validCustomers = _context.CustomerMains
            .Where(c => c.STATUS_FLAG == 1)
            .Select(c => c.MAIN_CUST)
            .ToList()
            .Where(c => c != null)
            .Select(c => c!.Trim().ToUpperInvariant())
            .ToHashSet();

        var validOrigins = _context.Origins
            .Select(o => o.origin_code)
            .ToList()
            .Where(o => o != null)
            .Select(o => o!.Trim().ToUpperInvariant())
            .ToHashSet();

        var validDestinations = _context.Destinations
            .Select(d => d.destination_code)
            .ToList()
            .Where(d => d != null)
            .Select(d => d!.Trim().ToUpperInvariant())
            .ToHashSet();

        var validServTypes = _context.ServiceTypes
            .Select(s => s.serv_name)
            .ToList()
            .Where(s => s != null)
            .Select(s => s!.Trim().ToUpperInvariant())
            .ToHashSet();

        var validServModas = _context.ServiceModas
            .Select(s => s.moda_name)
            .ToList()
            .Where(s => s != null)
            .Select(s => s!.Trim().ToUpperInvariant())
            .ToHashSet();

        var validTruckSizes = _context.TruckSizes
            .Select(t => t.trucksize_code)
            .ToList()
            .Where(t => t != null)
            .Select(t => t!.Trim().ToUpperInvariant())
            .ToHashSet();

        // Existing composite key: cust_code|origin|dest|serv_type|serv_moda|truck_size
        var existingKeys = _context.LeadTimes
            .Select(l => new { l.cust_code, l.origin, l.dest, l.serv_type, l.serv_moda, l.truck_size })
            .ToList()
            .Select(l => BuildKey(l.cust_code, l.origin, l.dest, l.serv_type, l.serv_moda, l.truck_size))
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
                var custCode = row.Cell(1).GetValue<string>()?.Trim();
                var origin = row.Cell(2).GetValue<string>()?.Trim();
                var dest = row.Cell(3).GetValue<string>()?.Trim();
                var servType = row.Cell(4).GetValue<string>()?.Trim();
                var servModa = row.Cell(5).GetValue<string>()?.Trim();
                var truckSize = row.Cell(6).GetValue<string>()?.Trim();
                var deliveryDaysText = row.Cell(7).GetValue<string>()?.Trim();
                var podDaysText = row.Cell(8).GetValue<string>()?.Trim();

                if (string.IsNullOrWhiteSpace(custCode) && string.IsNullOrWhiteSpace(origin) && string.IsNullOrWhiteSpace(dest))
                    continue; // empty row, skip silently

                var identifier = $"{custCode}/{origin}/{dest}";

                // Required fields
                var requiredErrors = new List<string>();
                if (string.IsNullOrWhiteSpace(custCode)) requiredErrors.Add("Customer is required");
                if (string.IsNullOrWhiteSpace(origin)) requiredErrors.Add("Origin is required");
                if (string.IsNullOrWhiteSpace(dest)) requiredErrors.Add("Destination is required");

                if (requiredErrors.Any())
                {
                    invalid.Add($"{identifier} ({string.Join(", ", requiredErrors)})");
                    continue;
                }

                // Length validation
                var lengthErrors = new List<string>();
                if (custCode!.Length > 30) lengthErrors.Add("Customer > 30 chars");
                if (origin!.Length > 100) lengthErrors.Add("Origin > 100 chars");
                if (dest!.Length > 100) lengthErrors.Add("Destination > 100 chars");
                if (servType?.Length > 10) lengthErrors.Add("Serv Type > 10 chars");
                if (servModa?.Length > 10) lengthErrors.Add("Serv Moda > 10 chars");
                if (truckSize?.Length > 50) lengthErrors.Add("Truck Size > 50 chars");

                // Numeric validation
                int? deliveryDays = null, podDays = null;
                var numErrors = new List<string>();

                if (!string.IsNullOrWhiteSpace(deliveryDaysText))
                {
                    if (!int.TryParse(deliveryDaysText, out var dd) || dd < 0)
                        numErrors.Add("Delivery Days must be a non-negative whole number");
                    else
                        deliveryDays = dd;
                }

                if (!string.IsNullOrWhiteSpace(podDaysText))
                {
                    if (!int.TryParse(podDaysText, out var pd) || pd < 0)
                        numErrors.Add("POD Days must be a non-negative whole number");
                    else
                        podDays = pd;
                }

                if (lengthErrors.Any() || numErrors.Any())
                {
                    var allErrors = lengthErrors.Concat(numErrors);
                    invalid.Add($"{identifier} ({string.Join(", ", allErrors)})");
                    continue;
                }

                // Master validation — skip if not found
                if (!validCustomers.Contains(custCode.ToUpperInvariant()))
                {
                    skippedCustomerNotFound.Add($"{identifier} (Customer '{custCode}' not found or inactive)");
                    continue;
                }

                if (!validOrigins.Contains(origin.ToUpperInvariant()))
                {
                    skippedOriginNotFound.Add($"{identifier} (Origin '{origin}' not found)");
                    continue;
                }

                if (!validDestinations.Contains(dest.ToUpperInvariant()))
                {
                    skippedDestNotFound.Add($"{identifier} (Destination '{dest}' not found)");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(servType) && !validServTypes.Contains(servType.ToUpperInvariant()))
                {
                    skippedServTypeNotFound.Add($"{identifier} (Serv Type '{servType}' not found)");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(servModa) && !validServModas.Contains(servModa.ToUpperInvariant()))
                {
                    skippedServModaNotFound.Add($"{identifier} (Serv Moda '{servModa}' not found)");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(truckSize) && !validTruckSizes.Contains(truckSize.ToUpperInvariant()))
                {
                    skippedTruckSizeNotFound.Add($"{identifier} (Truck Size '{truckSize}' not found)");
                    continue;
                }

                // Composite key check
                var compositeKey = BuildKey(custCode, origin, dest, servType, servModa, truckSize);

                if (existingKeys.Contains(compositeKey))
                {
                    skippedExisting.Add(identifier);
                    continue;
                }

                if (seenInFile.Contains(compositeKey))
                {
                    skippedDuplicateInFile.Add(identifier);
                    continue;
                }

                seenInFile.Add(compositeKey);

                _context.LeadTimes.Add(new LeadTime
                {
                    cust_code = custCode,
                    origin = origin,
                    dest = dest,
                    serv_type = servType,
                    serv_moda = servModa,
                    truck_size = truckSize,
                    delivery_days = deliveryDays,
                    pod_days = podDays,
                    active_flag = 1,
                    entry_user = username,
                    entry_date = now
                });

                added.Add(identifier);
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
                + skippedOriginNotFound.Count + skippedDestNotFound.Count + skippedServTypeNotFound.Count
                + skippedServModaNotFound.Count + skippedTruckSizeNotFound.Count
                + skippedDuplicateInFile.Count + invalid.Count,
            added,
            skippedExisting,
            skippedCustomerNotFound,
            skippedOriginNotFound,
            skippedDestNotFound,
            skippedServTypeNotFound,
            skippedServModaNotFound,
            skippedTruckSizeNotFound,
            skippedDuplicateInFile,
            invalid
        });
    }

    // Normalisasi composite key — field opsional yang kosong dianggap seragam ("")
    private static string BuildKey(string? custCode, string? origin, string? dest,
        string? servType, string? servModa, string? truckSize)
    {
        string N(string? s) => (s ?? "").Trim().ToUpperInvariant();
        return $"{N(custCode)}|{N(origin)}|{N(dest)}|{N(servType)}|{N(servModa)}|{N(truckSize)}";
    }

    public IActionResult DownloadTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("LeadTime Template");

        string[] headers = {
        "Customer", "Origin", "Destination", "Serv Type", "Serv Moda", "Truck Size",
        "Delivery Days", "POD Days"
    };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Example row
        var sampleCust = _context.CustomerMains.Where(c => c.STATUS_FLAG == 1).Select(c => c.MAIN_CUST).FirstOrDefault();
        var sampleOrigin = _context.Origins.Select(o => o.origin_code).FirstOrDefault();
        var sampleDest = _context.Destinations.Select(d => d.destination_code).FirstOrDefault();

        worksheet.Cell(2, 1).Value = sampleCust ?? "CUST001";
        worksheet.Cell(2, 2).Value = sampleOrigin ?? "Jakarta Warehouse";
        worksheet.Cell(2, 3).Value = sampleDest ?? "Warehouse Cikarang";
        worksheet.Cell(2, 7).Value = 2;
        worksheet.Cell(2, 8).Value = 5;

        worksheet.Columns().AdjustToContents();

        // Reference sheet
        var refSheet = workbook.Worksheets.Add("Reference Lists");
        refSheet.Cell(1, 1).Value = "Valid Customers (Active)";
        refSheet.Cell(1, 2).Value = "Valid Origins";
        refSheet.Cell(1, 3).Value = "Valid Destinations";
        refSheet.Cell(1, 4).Value = "Valid Serv Type";
        refSheet.Cell(1, 5).Value = "Valid Serv Moda";
        refSheet.Cell(1, 6).Value = "Valid Truck Size";
        refSheet.Range(1, 1, 1, 6).Style.Font.Bold = true;

        var custList = _context.CustomerMains.Where(c => c.STATUS_FLAG == 1).Select(c => c.MAIN_CUST).ToList();
        var originList = _context.Origins.Select(o => o.origin_code).ToList();
        var destList = _context.Destinations.Select(d => d.destination_code).ToList();
        var servTypeList = _context.ServiceTypes.Select(s => s.serv_name).ToList();
        var servModaList = _context.ServiceModas.Select(s => s.moda_name).ToList();
        var truckSizeList = _context.TruckSizes.Select(t => t.trucksize_code).ToList();

        for (int i = 0; i < custList.Count; i++) refSheet.Cell(i + 2, 1).Value = custList[i];
        for (int i = 0; i < originList.Count; i++) refSheet.Cell(i + 2, 2).Value = originList[i];
        for (int i = 0; i < destList.Count; i++) refSheet.Cell(i + 2, 3).Value = destList[i];
        for (int i = 0; i < servTypeList.Count; i++) refSheet.Cell(i + 2, 4).Value = servTypeList[i];
        for (int i = 0; i < servModaList.Count; i++) refSheet.Cell(i + 2, 5).Value = servModaList[i];
        for (int i = 0; i < truckSizeList.Count; i++) refSheet.Cell(i + 2, 6).Value = truckSizeList[i];

        refSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "LeadTimeUploadTemplate.xlsx");
    }

    // ── Export ──────────────────────────────────────────────────────────────
    public IActionResult Export(string? custCode)
    {
        var query = _context.LeadTimes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(custCode) && custCode != "ALL")
        {
            query = query.Where(l => l.cust_code == custCode);
        }

        var list = query.OrderBy(l => l.cust_code).ThenBy(l => l.origin).ThenBy(l => l.dest).ToList();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Lead Times");

        string[] headers = {
            "Customer", "Origin", "Destination", "Serv Type", "Serv Moda", "Truck Size",
            "Delivery Days", "POD Days", "Active"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var item in list)
        {
            worksheet.Cell(row, 1).Value = item.cust_code ?? "";
            worksheet.Cell(row, 2).Value = item.origin ?? "";
            worksheet.Cell(row, 3).Value = item.dest ?? "";
            worksheet.Cell(row, 4).Value = item.serv_type ?? "";
            worksheet.Cell(row, 5).Value = item.serv_moda ?? "";
            worksheet.Cell(row, 6).Value = item.truck_size ?? "";
            worksheet.Cell(row, 7).Value = item.delivery_days ?? (int?)null;
            worksheet.Cell(row, 8).Value = item.pod_days ?? (int?)null;
            worksheet.Cell(row, 9).Value = item.active_flag == 1 ? "Active" : "Inactive";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = string.IsNullOrWhiteSpace(custCode) || custCode == "ALL"
            ? "LeadTime_AllCustomers.xlsx"
            : $"LeadTime_{custCode}.xlsx";

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet]
    public IActionResult GetActiveCustomers()
    {
        var list = _context.CustomerMains
            .Where(c => c.STATUS_FLAG == 1)
            .OrderBy(c => c.MAIN_CUST)
            .Select(c => new { c.MAIN_CUST, c.CUST_NAME })
            .ToList();

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = null // matikan camelCase, pakai nama asli persis
        };

        return new JsonResult(list, options);
    }
}