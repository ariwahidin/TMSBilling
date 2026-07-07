using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Services;
using ClosedXML.Excel;
using System.Globalization;


namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class GeofenceController : Controller
    {
        private readonly ILogger<GeofenceController> _logger;
        private readonly AppDbContext _context;
        private readonly SelectListService _selectList;
        private readonly ApiService _apiService;

        public GeofenceController(ILogger<GeofenceController> logger,
            ApiService apiService,
            AppDbContext context,
            SelectListService selectList)
        {
            _logger = logger;
            _apiService = apiService;
            _context = context;
            _selectList = selectList;
        }


        // GET: /Geofence

        [HttpGet]
        public async Task<IActionResult> GetGeofenceByCategory(string? id, String? category)
        {

            Console.WriteLine($"Customer ID from query: {id}");
            Console.WriteLine($"Category from query: {category}");

            if (id == null)
            {
                return BadRequest("Id tidak boleh kosong");
            }


            var customerGroup = await _context.CustomerGroups
                .Where(c => c.SUB_CODE == id)
                .FirstOrDefaultAsync();

            if (customerGroup == null)
            {
                return NotFound(new { message = "Customer group not found" });
            }

            var customer = await _context.Customers
                .Where(c => c.CUST_CODE == customerGroup.CUST_CODE)
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { message = "Customer not found" });
            }


            var username = HttpContext.Session.GetString("username") ?? "System";

            var customerGroups = _context.UserXCustomers
                                .Where(x => x.UserName == username)
                                .Select(x => x.CustomerMain)
                                .Distinct()
                                .Join(_context.CustomerGroups,
                                      custMain => custMain,
                                      cg => cg.MAIN_CUST,
                                      (custMain, cg) => cg)
                                .ToList();

            var customerGroupAllowed = customerGroups
                .Select(x => x.SUB_CODE)
                .Distinct()
                .ToList();


            if (customer.API_FLAG == 1)
            {

                var consignees = await _context.Geofences
                .Where(c => c.CustomerName == id && customerGroupAllowed.Contains(c.CustomerName))
                .ToListAsync();

                var geofenceList = consignees.Select(c => new GeofenceViewModel
                {
                    FenceID = c.FenceName,
                    FenceName = c.FenceName,
                    Description = c.Description,
                    Category = c.Category,
                    Customer = c.CustomerName,
                    Address = c.Address,
                    City = c.City,
                    AddressDetail = c.Address,
                    PhoneNo = c.PhoneNo,
                    ContactName = c.ContactName,
                    CustomerName = c.CustomerName,
                    MCEasyCustId = c.CustomerId,
                    GeofenceId = c.GeofenceId?.ToString(),
                    CompanyId = c.CompanyId?.ToString(),
                    CUST_GROUP_CODE = c.CustomerName,
                    Province = c.Province,
                    PostalCode = c.PostalCode,
                    


                    Coordinates = null,
                    Radius = null,
                    ServiceStart = null,
                    ServiceEnd = null,
                    BreakStart = null,
                    BreakEnd = null,
                    ServiceLocType = null,
                    IsServiceLoc = null,
                    IsBillingAddr = null,
                    IsDepot = null,
                    IsAlert = null,
                    Type = null,
                    PolyData = null,
                    CircData = null,
                    HasRelation = null
                }).ToList();

                return Ok(new
                {
                    success = true,
                    message = "Data dari DB API",
                    data = geofenceList
                });

            }
            else {

                var consignees = await _context.Geofences
                    .Where(a => customerGroupAllowed.Contains(a.CustomerName))
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "Data dari Consignee",
                    data = consignees
                });
            }
        }

        // GET: /Geofence/Create
        public async Task<IActionResult> Create(Guid? id, String? category)
        {
            Console.WriteLine($"Customer ID from querys: {id}");

            if (id == null)
            {
                return BadRequest("Id tidak boleh kosong");
            }

            var customer = await _context.CustomerGroups
                .Where(c => c.MCEASY_CUST_ID == id.ToString())
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { message = "Customer Group tidak ditemukan" });
            }

            ViewBag.CustomerId = id.Value;
            ViewBag.CustomerName = customer.SUB_CODE; // <- ganti sesuai field yg ada
            ViewBag.ListCategory = _selectList.GeofenceCategory();
            ViewBag.ListCustomerGroupGeofence = _context.CustomerGroups
                .Where(c => c.MCEASY_CUST_ID != null)
                .Select(c => new SelectListItem
                {
                    Value = c.MCEASY_CUST_ID,
                    Text = c.SUB_CODE,
                    Selected = (c.MCEASY_CUST_ID == customer.MCEASY_CUST_ID) // set default
                }).ToList();
            var model = new GeofenceViewModel
            {
                MCEasyCustId = customer.MCEASY_CUST_ID
            };
            return View("Form", model);
        }

        public IActionResult Form(String? category, int? id) {
            ViewBag.Category = category;
            ViewBag.ListCategory = _selectList.GeofenceCategory();
            ViewBag.ListArea = _selectList.GetArea();
            ViewBag.ListCustomerGroupGeofence = _selectList.getCustomerGroup();
            var model = new GeofenceViewModel();
            model.ID = 0;
            model.GeofenceId = "0";

            if (id != null) {
                var consignee = _context.Geofences.FirstOrDefault(o => o.Id == id);
                if (consignee == null)
                {
                    return View("Form", model);
                }
                else
                {
                    model.ID = consignee.Id;
                    model.GeofenceId = consignee.GeofenceId.ToString();
                    model.FenceID = consignee.FenceName;
                    model.FenceName = consignee.FenceName;
                    model.Description = consignee.Description;
                    model.CUST_GROUP_CODE = consignee.CustomerName;
                    model.Address = consignee.Address;
                    model.PostalCode = consignee.PostalCode;
                    model.Coordinates = consignee.Cordinates;
                    model.Radius = consignee.Radius;
                    model.ContactName = consignee.ContactName;
                    model.Province = consignee.Province;
                    model.PhoneNo = consignee.PhoneNo;
                    model.City = consignee.City;
                    model.IsGarage = consignee.IsGarage;
                    model.Lat = consignee.Lat;
                    model.Lng = consignee.Long;
                }
            }

            return View("Form", model);
        }


        [HttpPost]
        public async Task<IActionResult> SaveGeofence([FromBody] GeofenceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "All fields must be filled in" });
            }

            model.Category = "Customer";

            var customerGroup = await _context.CustomerGroups
                .Where(c => c.SUB_CODE == model.CUST_GROUP_CODE)
                .FirstOrDefaultAsync();

            if (customerGroup == null)
            {
                return NotFound(new { message = "Customer group not found" });
            }

            var customer = await _context.Customers
                .Where(c => c.CUST_CODE == customerGroup.CUST_CODE)
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { message = "Customer not found" });
            }

            var groupCustomer = customerGroup.SUB_CODE;
            var user = HttpContext.Session.GetString("username") ?? "System";
            var date = DateTime.Now;

            if (model.ID > 0)
            {
                // EDIT
                var ng = await _context.Geofences
                    .FirstOrDefaultAsync(c => c.Id == model.ID);

                if (ng == null)
                {
                    return NotFound(new { message = "Consignee not found" });
                }



                // Kalau pakai API external juga update
                if (customerGroup.API_FLAG == 1 && ng.GeofenceId > 0)
                {

                    var graphqlVariables = new
                    {
                        input = new
                        {
                            geofenceId = ng.GeofenceId,
                            oldData = new
                            {
                                fenceName = ng.FenceName,
                                city = ng.City,
                                address = ng.Address,
                                postalCode = ng.PostalCode,
                                province = ng.Province,
                                category = ng.Category,
                                contactName = ng.ContactName,
                                phoneNo = ng.PhoneNo,
                                circData = $"<{ng.Cordinates},{ng.Radius}>",
                                isGarage = ng.IsGarage,
                            },
                            newData = new
                            {
                                fenceName = model.FenceName,
                                city = model.City,
                                address = model.Address,
                                postalCode = model.PostalCode,
                                province = model.Province,
                                category = model.Category,
                                contactName = model.ContactName,
                                phoneNo = model.PhoneNo,
                                circData = $"<{model.Coordinates},{model.Radius}>",
                                isGarage = model.IsGarage,
                            }
                        }
                    };

                    var (ok, result) = await _apiService.ExecuteGraphQLAsync(
                        @"mutation UpdateGeofence($input: UpdateGeofenceInput!) {
                            updateGeofence(input: $input) {
                                isSuccessful
                                message
                                geofence {
                                    geofenceId
                                    companyId
                                    customerId
                                    fenceName
                                    type
                                    polyData
                                    circData
                                    address
                                    addressDetail
                                    province
                                    city
                                    postalCode
                                    category
                                    contactName
                                    phoneNo
                                    isGarage
                                    isServiceLoc
                                    isBillingAddr
                                    isDepot
                                    isAlert
                                    serviceStart
                                    serviceEnd
                                    breakStart
                                    breakEnd
                                    serviceLocType
                                    customerName
                                    createdOn
                                    hasRelation
                                    contacts {
                                      contactId
                                      name
                                      phoneNo
                                      sendWhatsapp
                                    }
                                }
                            }
                        }",
                        graphqlVariables,
                        "UpdateGeofence"
                    );

                    if (!ok)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "GraphQL update request failed",
                            detail = result.ToString()
                        });
                    }

                    var updateGeofence = result.GetProperty("data").GetProperty("updateGeofence");

                    var success = updateGeofence.GetProperty("isSuccessful").GetBoolean();

                    if (!success)
                    {
                        var message = updateGeofence.GetProperty("message").GetString();

                        return BadRequest(new
                        {
                            success = false,
                            message
                        });
                    }

                    var p = updateGeofence
                             .GetProperty("geofence")
                             .Deserialize<Geofence>() ?? new Geofence();

                    if (p.circData != null)
                    {
                        var match = Regex.Match(p.circData, @"<\(\s*([-\d\.]+)\s*,\s*([-\d\.]+)\s*\)\s*,\s*(\d+)\s*>");

                        if (match.Success)
                        {
                            ng.Lat = $"{match.Groups[1].Value}";
                            ng.Long = $"{match.Groups[2].Value}";
                            ng.Cordinates = $"({ng.Lat},{ng.Long})";
                            ng.Radius = $"{match.Groups[3].Value}";
                        }
                    }

                    // Update fields

                    ng.CompanyId = p.companyId;
                    ng.CustomerId = p.customerId;
                    ng.FenceName = p.fenceName;
                    ng.Description = model.Description;
                    ng.Type = p.type;
                    ng.PolyData = model.PolyData;
                    ng.CircData = $"<{model.Coordinates},{model.Radius}>";
                    ng.Address = p.address;
                    ng.AddressDetail = p.addressDetail;
                    ng.Province = p.province;
                    ng.City = p.city;
                    ng.PostalCode = p.postalCode;
                    ng.Category = p.category;
                    ng.ContactName = p.contactName;
                    ng.PhoneNo = p.phoneNo;
                    ng.IsGarage = model.IsGarage;
                    ng.IsServiceLoc = p.isServiceLoc;
                    ng.IsBillingAddr = p.isBillingAddr;
                    ng.IsDepot = p.isDepot;
                    ng.IsAlert = p.isAlert;
                    ng.ServiceStart = p.serviceStart;
                    ng.ServiceEnd = p.serviceEnd;
                    ng.BreakStart = p.breakStart;
                    ng.BreakEnd = p.breakEnd;
                    ng.ServiceLocType = p.serviceLocType;
                    ng.CustomerName = p.customerName;
                    ng.HasRelation = p.hasRelation;
                    ng.UpdatedAt = DateTime.Now;
                }
                else {

                   
                    

                    ng.FenceName = model.FenceName;
                    ng.Description = model.Description;
                    ng.City = model.City;
                    ng.Address = model.Address;
                    ng.PostalCode = model.PostalCode;
                    ng.Province = model.Province;
                    ng.Category = model.Category;
                    ng.ContactName = model.ContactName;
                    ng.PhoneNo = model.PhoneNo;
                    ng.CircData = $"<{model.Coordinates},{model.Radius}>";
                    ng.IsGarage = model.IsGarage;



                    var match = Regex.Match(ng.CircData, @"<\(\s*([-\d\.]+)\s*,\s*([-\d\.]+)\s*\)\s*,\s*(\d+)\s*>");

                    if (match.Success)
                    {
                        ng.Lat = $"{match.Groups[1].Value}";
                        ng.Long = $"{match.Groups[2].Value}";
                        ng.Cordinates = $"({ng.Lat},{ng.Long})";
                        ng.Radius = $"{match.Groups[3].Value}";
                    }
                    ng.CustomerName = customerGroup.SUB_CODE;

                    ng.CreatedAt = DateTime.Now;
                    ng.UpdatedAt = DateTime.Now;

                }
            }
            else
            {

                // CREATE
                var ng = new Models.GeofenceTable{};

                var graphqlVariables = new
                {
                    input = new
                    {
                        circData = $"<{model.Coordinates},{model.Radius}>",
                        polyData = string.IsNullOrEmpty(model.PolyData) ? null : model.PolyData,
                        fenceName = model.FenceName,
                        type = model.Type,
                        address = model.Address,
                        addressDetail = model.AddressDetail,
                        province = model.Province,
                        city = model.City,
                        postalCode = string.IsNullOrEmpty(model.PostalCode) ? null : model.PostalCode,
                        category = model.Category,
                        contactName = string.IsNullOrEmpty(model.ContactName) ? null : model.ContactName,
                        phoneNo = string.IsNullOrEmpty(model.PhoneNo) ? null : model.PhoneNo,
                        serviceStart = string.IsNullOrEmpty(model.ServiceStart) ? null : DateTime.Parse(model.ServiceStart).ToString("HH:mm:ss"),
                        serviceEnd = string.IsNullOrEmpty(model.ServiceEnd) ? null : DateTime.Parse(model.ServiceEnd).ToString("HH:mm:ss"),
                        breakStart = string.IsNullOrEmpty(model.BreakStart) ? null : model.BreakStart,
                        breakEnd = string.IsNullOrEmpty(model.BreakEnd) ? null : model.BreakEnd,
                        isDepot = model.IsDepot == "true",
                        isBillingAddr = model.IsBillingAddr == "true",
                        customerName = customerGroup.SUB_CODE,
                        customerId = customerGroup.MCEASY_CUST_ID,
                        isGarage = model.IsGarage,
                    }
                };

                if (graphqlVariables.input.circData != null)
                {
                    var match = Regex.Match(graphqlVariables.input.circData, @"<\(\s*([-\d\.]+)\s*,\s*([-\d\.]+)\s*\)\s*,\s*(\d+)\s*>");

                    if (match.Success)
                    {
                        ng.Lat = $"{match.Groups[1].Value}";
                        ng.Long = $"{match.Groups[2].Value}";
                        ng.Cordinates = $"({ng.Lat},{ng.Long})";
                        ng.Radius = $"{match.Groups[3].Value}";
                    }
                }

                if (customerGroup.API_FLAG == 1)
                {
                    var (ok, result) = await _apiService.ExecuteGraphQLAsync(
                        @"mutation CreateGeofence($input: CreateGeofenceInput!) {
                            createGeofence(input: $input) {
                                isSuccessful
                                message
                                geofence {
                                    geofenceId
                                    companyId
                                    customerId
                                    fenceName
                                    type
                                    polyData
                                    circData
                                    address
                                    addressDetail
                                    province
                                    city
                                    postalCode
                                    category
                                    contactName
                                    phoneNo
                                    isGarage
                                    isServiceLoc
                                    isBillingAddr
                                    isDepot
                                    isAlert
                                    serviceStart
                                    serviceEnd
                                    breakStart
                                    breakEnd
                                    serviceLocType
                                    customerName
                                    createdOn
                                    hasRelation
                                    contacts {
                                      contactId
                                      name
                                      phoneNo
                                      sendWhatsapp
                                    }
                                }
                            }
                        }",
                        graphqlVariables,
                        "CreateGeofence"
                    );

                    if (!ok)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "GraphQL request failed",
                            detail = result.ToString()
                        });
                    }

                    var createGeofence = result.GetProperty("data").GetProperty("createGeofence");

                    var success = createGeofence.GetProperty("isSuccessful").GetBoolean();

                    if (!success)
                    {
                        var message = createGeofence.GetProperty("message").GetString();

                        return BadRequest(new
                        {
                            success = false,
                            message
                        });
                    }

                    var p = createGeofence
                         .GetProperty("geofence")
                         .Deserialize<Geofence>() ?? new Geofence();

                    ng.GeofenceId = p.geofenceId;
                    ng.CompanyId = p.companyId;
                    ng.CustomerId = p.customerId;
                    ng.FenceName = p.fenceName;
                    ng.Type = p.type;
                    ng.PolyData = p.polyData;
                    ng.CircData = p.circData;
                    ng.Address = p.address;
                    ng.AddressDetail = p.addressDetail;
                    ng.Province = p.province;
                    ng.City = p.city;
                    ng.PostalCode = p.postalCode;
                    ng.Category = p.category;
                    ng.ContactName = p.contactName;
                    ng.PhoneNo = p.phoneNo;
                    ng.IsGarage = p.isGarage;
                    ng.IsServiceLoc = p.isServiceLoc;
                    ng.IsBillingAddr = p.isBillingAddr;
                    ng.IsDepot = p.isDepot;
                    ng.IsAlert = p.isAlert;
                    ng.ServiceStart = p.serviceStart;
                    ng.ServiceEnd = p.serviceEnd;
                    ng.BreakStart = p.breakStart;
                    ng.BreakEnd = p.breakEnd;
                    ng.ServiceLocType = p.serviceLocType;
                    ng.CustomerName = p.customerName;
                    ng.HasRelation = p.hasRelation;
                    ng.CreatedAt = DateTime.Now;
                    ng.UpdatedAt = DateTime.Now;

                }
                else {

                    var p = graphqlVariables.input;

                    //ng.GeofenceId = p.geofenceId;
                    //ng.CompanyId = p.companyId;
                    //ng.CustomerId = p.customerId;
                    ng.FenceName = p.fenceName;
                    ng.Type = p.type;
                    ng.PolyData = p.polyData;
                    ng.CircData = p.circData;
                    ng.Address = p.address;
                    ng.AddressDetail = p.addressDetail;
                    ng.Province = p.province;
                    ng.City = p.city;
                    ng.PostalCode = p.postalCode;
                    ng.Category = p.category;
                    ng.ContactName = p.contactName;
                    ng.PhoneNo = p.phoneNo;
                    ng.IsGarage = p.isGarage;
                    //ng.IsServiceLoc = p.isServiceLoc;
                    ng.IsBillingAddr = p.isBillingAddr;
                    ng.IsDepot = p.isDepot;
                    //ng.IsAlert = p.isAlert;
                    ng.ServiceStart = p.serviceStart;
                    ng.ServiceEnd = p.serviceEnd;
                    ng.BreakStart = p.breakStart;
                    ng.BreakEnd = p.breakEnd;
                    //ng.ServiceLocType = p.serviceLocType;
                    ng.CustomerName = p.customerName;
                    //ng.HasRelation = p.hasRelation;
                    ng.CreatedAt = DateTime.Now;
                    ng.UpdatedAt = DateTime.Now;

                }

                _context.Geofences.Add(ng);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = model.ID > 0 ? "Consignee Updated Successfully" : "Consignee Created Successfully"
            });
        }


        // GET: /Geofence/Upload — halaman terpisah
        public IActionResult Upload()
        {
            return View();
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
            var skippedDuplicateInFile = new List<string>();
            var invalid = new List<string>();

            // Load master customer groups (validasi CUST_GROUP_CODE)
            var validCustomerCodes = _context.CustomerGroups
                .Select(c => c.SUB_CODE)
                .ToList()
                .Select(c => c.Trim().ToUpperInvariant())
                .ToHashSet();

            // Load existing geofence sebagai composite key: FenceName + CustomerName
            var existingGeofences = _context.Geofences
                .Select(g => new { g.FenceName, g.CustomerName })
                .ToList()
                .Where(g => g.FenceName != null && g.CustomerName != null)
                .Select(g => $"{g.FenceName!.Trim().ToUpperInvariant()}|{g.CustomerName!.Trim().ToUpperInvariant()}")
                .ToHashSet();

            var seenInFile = new HashSet<string>();
            var username = HttpContext.Session.GetString("username") ?? "System";
            var now = DateTime.Now;

            // Indonesia bounds — sama seperti INDONESIA_BOUNDS di map
            const double MinLat = -11.0, MaxLat = 6.0;
            const double MinLng = 95.0, MaxLng = 141.0;

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
                    var fenceName = row.Cell(1).GetValue<string>()?.Trim();
                    var description = row.Cell(2).GetValue<string>()?.Trim();
                    var custCode = row.Cell(3).GetValue<string>()?.Trim();
                    var address = row.Cell(4).GetValue<string>()?.Trim();
                    var province = row.Cell(5).GetValue<string>()?.Trim();
                    var city = row.Cell(6).GetValue<string>()?.Trim();
                    var postalCode = row.Cell(7).GetValue<string>()?.Trim();
                    var latText = row.Cell(8).GetValue<string>()?.Trim();
                    var lngText = row.Cell(9).GetValue<string>()?.Trim();
                    var radiusText = row.Cell(10).GetValue<string>()?.Trim();
                    var contactName = row.Cell(11).GetValue<string>()?.Trim();
                    var phoneNo = row.Cell(12).GetValue<string>()?.Trim();
                    var isGarageText = row.Cell(13).GetValue<string>()?.Trim();

                    if (string.IsNullOrWhiteSpace(fenceName) && string.IsNullOrWhiteSpace(custCode))
                        continue; // empty row, skip silently

                    var identifier = !string.IsNullOrWhiteSpace(fenceName) ? fenceName : "(no fence name)";

                    // Required fields
                    var requiredErrors = new List<string>();
                    if (string.IsNullOrWhiteSpace(fenceName)) requiredErrors.Add("Fence Name is required");
                    if (string.IsNullOrWhiteSpace(description)) requiredErrors.Add("Description is required");
                    if (string.IsNullOrWhiteSpace(custCode)) requiredErrors.Add("Customer Group Code is required");
                    //if (string.IsNullOrWhiteSpace(city)) requiredErrors.Add("City is required");

                    if (requiredErrors.Any())
                    {
                        invalid.Add($"{identifier} ({string.Join(", ", requiredErrors)})");
                        continue;
                    }

                    // Length validation (sesuai MaxLength di model)
                    var lengthErrors = new List<string>();
                    if (fenceName!.Length > 100) lengthErrors.Add("Fence Name > 100 chars");
    
                    if (address?.Length > 255) lengthErrors.Add("Address > 255 chars");
                    if (province?.Length > 100) lengthErrors.Add("Province > 100 chars");
                    if (city!.Length > 100) lengthErrors.Add("City > 100 chars");
                    if (postalCode?.Length > 10) lengthErrors.Add("Postal Code > 10 chars");
                    if (contactName?.Length > 100) lengthErrors.Add("Contact Name > 100 chars");
                    if (phoneNo?.Length > 20) lengthErrors.Add("Phone No > 20 chars");

                    // Lat/Lng validation
                    var coordErrors = new List<string>();
                    double lat = 0, lng = 0;

                    if (!string.IsNullOrWhiteSpace(latText) && !string.IsNullOrWhiteSpace(lngText))
                    {
                        if (!double.TryParse(latText, NumberStyles.Float, CultureInfo.InvariantCulture, out lat))
                            coordErrors.Add("Latitude is not a valid number");
                        else if (lat < MinLat || lat > MaxLat)
                            coordErrors.Add($"Latitude out of Indonesia range ({MinLat} to {MaxLat})");

                        if (!double.TryParse(lngText, NumberStyles.Float, CultureInfo.InvariantCulture, out lng))
                            coordErrors.Add("Longitude is not a valid number");
                        else if (lng < MinLng || lng > MaxLng)
                            coordErrors.Add($"Longitude out of Indonesia range ({MinLng} to {MaxLng})");
                    }

                        // Radius — optional, default 100
                        string radius = "100";
                    if (!string.IsNullOrWhiteSpace(radiusText))
                    {
                        if (!int.TryParse(radiusText, out var radiusInt) || radiusInt <= 0)
                        {
                            coordErrors.Add("Radius must be a positive whole number");
                        }
                        else
                        {
                            radius = radiusInt.ToString();
                        }
                    }

                    if (lengthErrors.Any() || coordErrors.Any())
                    {
                        var allErrors = lengthErrors.Concat(coordErrors);
                        invalid.Add($"{identifier} ({string.Join(", ", allErrors)})");
                        continue;
                    }

                    // Master validation — skip if not found
                    var normalizedCustCode = custCode!.ToUpperInvariant();
                    if (!validCustomerCodes.Contains(normalizedCustCode))
                    {
                        skippedCustomerNotFound.Add($"{fenceName} (Customer Group Code '{custCode}' not found)");
                        continue;
                    }

                    // Is Garage parsing — terima "true"/"false"/"1"/"0"/kosong
                    bool isGarage = false;
                    if (!string.IsNullOrWhiteSpace(isGarageText))
                    {
                        isGarage = isGarageText.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
                            || isGarageText.Trim() == "1";
                    }

                    // Composite key check
                    var compositeKey = $"{fenceName.ToUpperInvariant()}|{normalizedCustCode}";

                    if (existingGeofences.Contains(compositeKey))
                    {
                        skippedExisting.Add($"{fenceName} (Customer: {custCode})");
                        continue;
                    }

                    if (seenInFile.Contains(compositeKey))
                    {
                        skippedDuplicateInFile.Add($"{fenceName} (Customer: {custCode})");
                        continue;
                    }

                    seenInFile.Add(compositeKey);

                    var latStr = lat.ToString(CultureInfo.InvariantCulture);
                    var lngStr = lng.ToString(CultureInfo.InvariantCulture);

                    _context.Geofences.Add(new GeofenceTable
                    {
                        FenceName = fenceName,
                        Description = description,
                        CustomerName = custCode,
                        Address = address,
                        Province = province,
                        City = city,
                        PostalCode = postalCode,
                        Lat = latStr,
                        Long = lngStr,
                        Cordinates = $"({latStr},{lngStr})",
                        Radius = radius,
                        CircData = $"<({latStr},{lngStr}),{radius}>",
                        Type = "circle",
                        ContactName = contactName,
                        PhoneNo = phoneNo,
                        IsGarage = isGarage,
                        Category = "Customer",
                        CreatedAt = now,
                        UpdatedAt = now
                    });

                    added.Add($"{fenceName} (Customer: {custCode})");
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
                    + skippedDuplicateInFile.Count + invalid.Count,
                added,
                skippedExisting,
                skippedCustomerNotFound,
                skippedDuplicateInFile,
                invalid
            });
        }

        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Geofence Template");

            string[] headers = {
                "Fence Name", "Description", "Customer Group Code", "Address", "Province", "City",
                "Postal Code", "Latitude", "Longitude", "Radius", "Contact Name", "Phone No", "Is Garage"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Example row
            worksheet.Cell(2, 1).Value = "Warehouse Cikarang";
            worksheet.Cell(2, 2).Value = "Description of Warehouse Cikarang";
            worksheet.Cell(2, 3).Value = "CUST001";
            worksheet.Cell(2, 4).Value = "Jl. Contoh No. 1";
            worksheet.Cell(2, 5).Value = "Jawa Barat";
            worksheet.Cell(2, 6).Value = "Cikarang";
            worksheet.Cell(2, 7).Value = "17530";
            worksheet.Cell(2, 8).Value = "-6.2088";
            worksheet.Cell(2, 9).Value = "106.8456";
            worksheet.Cell(2, 10).Value = "100";
            worksheet.Cell(2, 11).Value = "John Doe";
            worksheet.Cell(2, 12).Value = "0812345678";
            worksheet.Cell(2, 13).Value = "false";

            worksheet.Columns().AdjustToContents();

            // Reference sheet — daftar customer group code yang valid
            var refSheet = workbook.Worksheets.Add("Reference Lists");
            refSheet.Cell(1, 1).Value = "Valid Customer Group Codes";
            refSheet.Cell(1, 1).Style.Font.Bold = true;

            var custCodes = _context.CustomerGroups.Select(c => c.SUB_CODE).ToList();
            for (int i = 0; i < custCodes.Count; i++)
                refSheet.Cell(i + 2, 1).Value = custCodes[i];

            refSheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "GeofenceUploadTemplate.xlsx");
        }

    }

    public class GeofenceViewModel
    {
        public int? ID { get; set; }
        
        public string? FenceID { get; set; }
        [Required]
        public string? FenceName { get; set; }

        [Required]
        public string? Description { get; set; } = null;

        public string? Customer {  get; set; }

        public string? Category { get; set; }
        public string? Address { get; set; }
        public string? Province { get; set; }
        [Required]
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Coordinates { get; set; }
        public string? Radius { get; set; }
        public string? AddressDetail { get; set; }
        public string? ContactName { get; set; }
        public string? PhoneNo { get; set; }
        public string? CustomerName { get; set; }
        public string? ServiceStart { get; set; }
        public string? ServiceEnd { get; set; }
        public string? BreakStart { get; set; }
        public string? BreakEnd { get; set; }
        public string? ServiceLocType { get; set; }
        public bool IsGarage { get; set; } = false;
        public string? IsServiceLoc { get; set; }
        public string? IsBillingAddr { get; set; }
        public string? IsDepot { get; set; }
        public string? IsAlert { get; set; }
        public string? GeofenceId { get; set; }
        public string? CompanyId { get; set; }
        public string? Type { get; set; }
        public string? PolyData { get; set; }
        public string? CircData { get; set; }
        public string? CreatedOn { get; set; }
        public string? HasRelation { get; set; }
        public string? MCEasyCustId { get; set; }

        [Required]
        public string? CUST_GROUP_CODE { get; set; }
        public string? AreaGroup {  get; set; }

        public string? Lat {  get; set; }
        public string? Lng { get; set; }

    }

    public class GraphQlResponse<T>
    {
        public T? Data { get; set; }
        public List<GraphQlError>? Errors { get; set; }
    }

    public class GraphQlError
    {
        public string? Message { get; set; }
        public List<Location>? Locations { get; set; }
        public List<string>? Path { get; set; }
        public object? Extensions { get; set; }
    }

    public class Location
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public class CreateGeofenceResponse
    {
        public CreateGeofencePayload? CreateGeofence { get; set; }
    }

    public class CreateGeofencePayload
    {
        public bool IsSuccessful { get; set; }
        public string? Message { get; set; }
        public Geofence? Geofence { get; set; }
    }

    public class Geofence
    {
        public int geofenceId { get; set; }
        public int companyId { get; set; }
        public string? customerId { get; set; }
        public string? fenceName { get; set; }
        public string? type { get; set; }
        public string? polyData { get; set; }
        public string? circData { get; set; }
        public string? address { get; set; }
        public string? addressDetail { get; set; }
        public string? province { get; set; }
        public string? city { get; set; }
        public string? postalCode { get; set; }
        public string? category { get; set; }
        public string? contactName { get; set; }
        public string? phoneNo { get; set; }
        public bool isGarage { get; set; }
        public bool isServiceLoc { get; set; }
        public bool isBillingAddr { get; set; }
        public bool isDepot { get; set; }
        public bool isAlert { get; set; }
        public string? serviceStart { get; set; }
        public string? serviceEnd { get; set; }
        public string? breakStart { get; set; }
        public string? breakEnd { get; set; }
        public string? serviceLocType { get; set; }
        public string? customerName { get; set; }
        public bool? hasRelation { get; set; }
    }

}