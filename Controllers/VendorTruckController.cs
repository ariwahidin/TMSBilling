using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class VendorTruckController : Controller
{
    private readonly AppDbContext _context;
    private static readonly string[] ValidMerks = {
    "DAIHATSU", "GRANDMAX", "HINO", "ISUZU", "MITSUBISHI",
    "NISSAN", "SUZUKI", "TOYOTA", "UD QUESTER"
    };

        private static readonly string[] ValidTypes = {
        "BLIND VAN", "BUILT UP", "CARRY", "CDD", "CDD LONG",
        "CDE", "FUSO", "HINO", "L300", "TRONTON", "WINGBOX"
    };

        private static readonly string[] ValidDoorTypes = {
        "PINTU BELAKANG", "PINTU DEPAN", "WINGBOX"
    };

    public VendorTruckController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var list = _context.VendorTrucks.ToList();
        return View(list);
    }

    public IActionResult Form(int? id)
    {
        SetDropdownLists();

        if (id == null)
        {
            return View("Form", new VendorTruck
            {
                sup_code = string.Empty,
                vehicle_no = string.Empty,
            });
        }

        var data = _context.VendorTrucks.Find(id);

        if (data == null)
            return NotFound();

        return View("Form", data);
    }

    [HttpPost]
    public IActionResult Create(VendorTruck model)
    {
        SetDropdownLists();

        if (!ModelState.IsValid)
            return View("Form", model);

        var isDuplicate = _context.VendorTrucks.Any(x => x.vehicle_no == model.vehicle_no);
        if (isDuplicate)
        {
            ModelState.AddModelError("vehicle_no", "Truck ID already exists.");
            return View("Form", model);
        }

        model.entry_date = DateTime.Now;
        model.entry_user = HttpContext.Session.GetString("username") ?? "System";

        _context.VendorTrucks.Add(model);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }

    public IActionResult Edit(int id)
    {
        var data = _context.VendorTrucks.FirstOrDefault(x => x.ID == id);
        if (data == null) return NotFound();

        SetDropdownLists();
        return View("Form", data);
    }

    [HttpPost]
    public IActionResult Edit(VendorTruck model)
    {
        SetDropdownLists();

        if (!ModelState.IsValid)
            return View("Form", model);

        var existing = _context.VendorTrucks.FirstOrDefault(x => x.ID == model.ID);
        if (existing == null) return NotFound();

        var isDuplicate = _context.VendorTrucks.Any(x => x.vehicle_no == model.vehicle_no && x.ID != model.ID);
        if (isDuplicate)
        {
            ModelState.AddModelError("vehicle_no", "Truck ID already exists.");
            return View("Form", model);
        }

        existing.sup_code = model.sup_code;
        existing.vehicle_no = model.vehicle_no;
        existing.vehicle_merk = model.vehicle_merk;
        existing.vehicle_type = model.vehicle_type;
        existing.vehicle_doortype = model.vehicle_doortype;
        existing.vehicle_size = model.vehicle_size;
        existing.vehicle_driver = model.vehicle_driver;
        existing.vehicle_STNK = model.vehicle_STNK;
        existing.vehicle_STNK_exp = model.vehicle_STNK_exp;
        existing.vehicle_KIR = model.vehicle_KIR;
        existing.vehicle_KIR_exp = model.vehicle_KIR_exp;
        existing.vehicle_emisi = model.vehicle_emisi;
        existing.vehicle_KTP = model.vehicle_KTP;
        existing.vehicle_SIM = model.vehicle_SIM;
        existing.vehicle_remark = model.vehicle_remark;
        existing.vehicle_active = model.vehicle_active;

        existing.update_date = DateTime.Now;
        existing.update_user = HttpContext.Session.GetString("username") ?? "System";

        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.VendorTrucks.FirstOrDefault(x => x.ID == id);
        if (data == null) return NotFound();

        _context.VendorTrucks.Remove(data);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    private void SetDropdownLists()
    {
        ViewBag.ListVendor = _context.Vendors
            .Select(v => new SelectListItem
            {
                Value = v.SUP_CODE,
                Text = $"{v.SUP_CODE} - {v.SUP_NAME}"
            }).ToList();

        ViewBag.ListCapacity = _context.TruckSizes
            .Select(x => new SelectListItem
            {
                Value = x.trucksize_code,
                Text = x.trucksize_code
            }).ToList();

        ViewBag.ListMerk = ValidMerks
            .Select(m => new SelectListItem { Value = m, Text = m }).ToList();

        ViewBag.ListType = ValidTypes
            .Select(t => new SelectListItem { Value = t, Text = t }).ToList();

        ViewBag.ListDoorType = ValidDoorTypes
            .Select(d => new SelectListItem { Value = d, Text = d }).ToList();
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
        var skippedVendorNotFound = new List<string>();
        var skippedSizeNotFound = new List<string>();
        var skippedDuplicateInFile = new List<string>();
        var invalid = new List<string>();

        //var existingVehicleNos = _context.VendorTrucks
        //    .Select(v => v.vehicle_no)
        //    .ToList()
        //    .Select(c => c.Trim().ToUpperInvariant())
        //    .ToHashSet();

        var existingVehicles = _context.VendorTrucks
            .Select(v => new { v.sup_code, v.vehicle_no })
            .ToList()
            .Select(v => $"{v.sup_code.Trim().ToUpperInvariant()}|{v.vehicle_no.Trim().ToUpperInvariant()}")
            .ToHashSet();

        var validVendorCodes = _context.Vendors
            .Select(v => v.SUP_CODE)
            .ToList()
            .Select(c => c.Trim().ToUpperInvariant())
            .ToHashSet();

        var validSizeCodes = _context.TruckSizes
            .Select(t => t.trucksize_code)
            .ToList()
            .Select(c => c.Trim().ToUpperInvariant())
            .ToHashSet();

        var validMerksSet = ValidMerks.Select(m => m.ToUpperInvariant()).ToHashSet();
        var validTypesSet = ValidTypes.Select(t => t.ToUpperInvariant()).ToHashSet();
        var validDoorTypesSet = ValidDoorTypes.Select(d => d.ToUpperInvariant()).ToHashSet();

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
                var supCode = row.Cell(1).GetValue<string>()?.Trim();
                var vehicleNo = row.Cell(2).GetValue<string>()?.Trim();
                var merk = row.Cell(3).GetValue<string>()?.Trim();
                var type = row.Cell(4).GetValue<string>()?.Trim();
                var doorType = row.Cell(5).GetValue<string>()?.Trim();
                var size = row.Cell(6).GetValue<string>()?.Trim();
                var driver = row.Cell(7).GetValue<string>()?.Trim();
                var stnk = row.Cell(8).GetValue<string>()?.Trim();
                var stnkExpText = row.Cell(9).GetValue<string>()?.Trim();
                var kir = row.Cell(10).GetValue<string>()?.Trim();
                var kirExpText = row.Cell(11).GetValue<string>()?.Trim();
                var emisiText = row.Cell(12).GetValue<string>()?.Trim();
                var ktp = row.Cell(13).GetValue<string>()?.Trim();
                var simText = row.Cell(14).GetValue<string>()?.Trim();
                var remark = row.Cell(15).GetValue<string>()?.Trim();

                if (string.IsNullOrWhiteSpace(supCode) && string.IsNullOrWhiteSpace(vehicleNo))
                    continue; // empty row, skip silently

                var identifier = !string.IsNullOrWhiteSpace(vehicleNo) ? vehicleNo : "(no vehicle no)";

                // Required fields
                if (string.IsNullOrWhiteSpace(supCode) || string.IsNullOrWhiteSpace(vehicleNo))
                {
                    invalid.Add($"{identifier} (Vendor Code / Vehicle No is required)");
                    continue;
                }

                // Length validation
                var lengthErrors = new List<string>();
                if (supCode.Length > 50) lengthErrors.Add("Vendor Code > 50 chars");
                if (vehicleNo.Length > 20) lengthErrors.Add("Vehicle No > 20 chars");
                if (remark?.Length > 70) lengthErrors.Add("Remark > 70 chars");
                if (ktp?.Length > 30) lengthErrors.Add("KTP > 30 chars");

                // Date parsing (yyyy-MM-dd)
                DateTime? stnkExp = null, kirExp = null, emisi = null, sim = null;
                var dateErrors = new List<string>();

                //if (!TryParseDate(stnkExpText, out stnkExp)) dateErrors.Add("STNK Exp invalid date format");
                //if (!TryParseDate(kirExpText, out kirExp)) dateErrors.Add("KIR Exp invalid date format");
                //if (!TryParseDate(emisiText, out emisi)) dateErrors.Add("Emisi invalid date format");
                //if (!TryParseDate(simText, out sim)) dateErrors.Add("SIM invalid date format");

                if (!TryParseCellDate(row.Cell(9), out stnkExp)) dateErrors.Add("STNK Exp invalid date format");
                if (!TryParseCellDate(row.Cell(11), out kirExp)) dateErrors.Add("KIR Exp invalid date format");
                if (!TryParseCellDate(row.Cell(12), out emisi)) dateErrors.Add("Emisi invalid date format");
                if (!TryParseCellDate(row.Cell(14), out sim)) dateErrors.Add("SIM invalid date format");

                if (lengthErrors.Any() || dateErrors.Any())
                {
                    var allErrors = lengthErrors.Concat(dateErrors);
                    invalid.Add($"{identifier} ({string.Join(", ", allErrors)})");
                    continue;
                }

                var normalizedVehicleNo = vehicleNo.ToUpperInvariant();
                var normalizedSupCode = supCode.ToUpperInvariant();

                // Master validation — skip if not found
                if (!validVendorCodes.Contains(normalizedSupCode))
                {
                    skippedVendorNotFound.Add($"{vehicleNo} (Vendor Code '{supCode}' not found)");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(size) && !validSizeCodes.Contains(size.ToUpperInvariant()))
                {
                    skippedSizeNotFound.Add($"{vehicleNo} (Truck Size '{size}' not found)");
                    continue;
                }

                // Dropdown validation — wajib cocok persis (case-insensitive)
                if (!string.IsNullOrWhiteSpace(merk) && !validMerksSet.Contains(merk.ToUpperInvariant()))
                {
                    invalid.Add($"{vehicleNo} (Merk '{merk}' is not a valid option)");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(type) && !validTypesSet.Contains(type.ToUpperInvariant()))
                {
                    invalid.Add($"{vehicleNo} (Type '{type}' is not a valid option)");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(doorType) && !validDoorTypesSet.Contains(doorType.ToUpperInvariant()))
                {
                    invalid.Add($"{vehicleNo} (Door Type '{doorType}' is not a valid option)");
                    continue;
                }

                // Uniqueness check
                //if (existingVehicleNos.Contains(normalizedVehicleNo))
                //{
                //    skippedExisting.Add(vehicleNo);
                //    continue;
                //}

                var compositeKey = $"{normalizedSupCode}|{normalizedVehicleNo}";

                if (existingVehicles.Contains(compositeKey))
                {
                    skippedExisting.Add($"{vehicleNo} (Vendor: {supCode})");
                    continue;
                }

                if (seenInFile.Contains(compositeKey))
                {
                    skippedDuplicateInFile.Add($"{vehicleNo} (Vendor: {supCode})");
                    continue;
                }

                seenInFile.Add(compositeKey);

                if (seenInFile.Contains(normalizedVehicleNo))
                {
                    skippedDuplicateInFile.Add(vehicleNo);
                    continue;
                }

                seenInFile.Add(normalizedVehicleNo);

                // Normalize to matched case from valid list (biar konsisten disimpan uppercase standar)
                var matchedMerk = ValidMerks.FirstOrDefault(m => m.Equals(merk, StringComparison.OrdinalIgnoreCase));
                var matchedType = ValidTypes.FirstOrDefault(t => t.Equals(type, StringComparison.OrdinalIgnoreCase));
                var matchedDoorType = ValidDoorTypes.FirstOrDefault(d => d.Equals(doorType, StringComparison.OrdinalIgnoreCase));

                _context.VendorTrucks.Add(new VendorTruck
                {
                    sup_code = supCode,
                    vehicle_no = vehicleNo,
                    vehicle_merk = matchedMerk ?? merk,
                    vehicle_type = matchedType ?? type,
                    vehicle_doortype = matchedDoorType ?? doorType,
                    vehicle_size = size,
                    vehicle_driver = driver,
                    vehicle_STNK = stnk,
                    vehicle_STNK_exp = stnkExp,
                    vehicle_KIR = kir,
                    vehicle_KIR_exp = kirExp,
                    vehicle_emisi = emisi,
                    vehicle_KTP = ktp,
                    vehicle_SIM = sim,
                    vehicle_remark = remark,
                    vehicle_active = 1,
                    entry_user = username,
                    entry_date = now
                });

                added.Add(vehicleNo);
            }

            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to read Excel file: " + ex.Message });
        }

        return Ok(new
        {
            totalProcessed = added.Count + skippedExisting.Count + skippedVendorNotFound.Count
                + skippedSizeNotFound.Count + skippedDuplicateInFile.Count + invalid.Count,
            added,
            skippedExisting,
            skippedVendorNotFound,
            skippedSizeNotFound,
            skippedDuplicateInFile,
            invalid
        });
    }

    private static bool TryParseDate(string? text, out DateTime? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(text)) return true; // empty = valid (null)

        if (DateTime.TryParseExact(text, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var parsed))
        {
            result = parsed;
            return true;
        }
        return false;
    }

    public IActionResult DownloadTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("VendorTruck Template");

        string[] headers = {
        "Vendor Code", "Vehicle No", "Merk", "Type", "Door Type", "Size",
        "Driver", "STNK", "STNK Exp (yyyy-MM-dd)", "KIR", "KIR Exp (yyyy-MM-dd)",
        "Emisi (yyyy-MM-dd)", "KTP", "SIM (yyyy-MM-dd)", "Remark"
    };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Example row
        worksheet.Cell(2, 1).Value = "V001";
        worksheet.Cell(2, 2).Value = "B1234XYZ";
        worksheet.Cell(2, 3).Value = "HINO";
        worksheet.Cell(2, 4).Value = "CDD";
        worksheet.Cell(2, 5).Value = "PINTU BELAKANG";
        worksheet.Cell(2, 6).Value = "20FT";
        worksheet.Cell(2, 7).Value = "John Doe";
        worksheet.Cell(2, 9).Value = "2027-12-31";

        worksheet.Columns().AdjustToContents();

        // Reference sheet — list of valid values, biar user gampang cocokin
        var refSheet = workbook.Worksheets.Add("Reference Lists");
        refSheet.Cell(1, 1).Value = "Valid Vendor Codes";
        refSheet.Cell(1, 2).Value = "Valid Truck Sizes";
        refSheet.Cell(1, 3).Value = "Valid Merk";
        refSheet.Cell(1, 4).Value = "Valid Type";
        refSheet.Cell(1, 5).Value = "Valid Door Type";
        refSheet.Range(1, 1, 1, 5).Style.Font.Bold = true;

        var vendorCodes = _context.Vendors.Select(v => v.SUP_CODE).ToList();
        var sizeCodes = _context.TruckSizes.Select(t => t.trucksize_code).ToList();

        for (int i = 0; i < vendorCodes.Count; i++)
            refSheet.Cell(i + 2, 1).Value = vendorCodes[i];

        for (int i = 0; i < sizeCodes.Count; i++)
            refSheet.Cell(i + 2, 2).Value = sizeCodes[i];

        for (int i = 0; i < ValidMerks.Length; i++)
            refSheet.Cell(i + 2, 3).Value = ValidMerks[i];

        for (int i = 0; i < ValidTypes.Length; i++)
            refSheet.Cell(i + 2, 4).Value = ValidTypes[i];

        for (int i = 0; i < ValidDoorTypes.Length; i++)
            refSheet.Cell(i + 2, 5).Value = ValidDoorTypes[i];

        refSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "VendorTruckUploadTemplate.xlsx");
    }

    private static bool TryParseCellDate(IXLCell cell, out DateTime? result)
    {
        result = null;

        if (cell == null || cell.IsEmpty())
            return true; // kosong = valid (null)

        // Kalau cell terdeteksi sebagai Date oleh Excel/ClosedXML
        if (cell.DataType == XLDataType.DateTime)
        {
            var dt = cell.GetDateTime();

            // Anggap epoch 1900-01-01 sebagai "kosong" (bukan tanggal beneran)
            if (dt == new DateTime(1900, 1, 1))
            {
                result = null;
                return true;
            }

            result = dt;
            return true;
        }

        // Kalau cell berupa text, coba parse manual dengan format yyyy-MM-dd
        var text = cell.GetValue<string>()?.Trim();

        if (string.IsNullOrWhiteSpace(text) ||
            text.Equals("NULL", StringComparison.OrdinalIgnoreCase))
        {
            result = null;
            return true;
        }

        if (DateTime.TryParseExact(text, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var parsed))
        {
            result = parsed;
            return true;
        }

        return false; // format tidak dikenali
    }
}

