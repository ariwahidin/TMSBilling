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
using System.Text.RegularExpressions;
using System.Text.Json;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Services;


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

            if (customer.API_FLAG == 1)
            {

                var consignees = await _context.Geofences
                .Where(c => c.CustomerName == id)
                .ToListAsync();

                var geofenceList = consignees.Select(c => new GeofenceViewModel
                {
                    FenceID = c.FenceName,
                    FenceName = c.FenceName,
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
                    


                    // sisanya default / null (karena ga ada di table Consignee)
                    //PostalCode = null,
                    Coordinates = null,
                    Radius = null,
                    ServiceStart = null,
                    ServiceEnd = null,
                    BreakStart = null,
                    BreakEnd = null,
                    ServiceLocType = null,
                    //IsGarage = null,
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
                var consignees = await _context.Consignees.ToListAsync();

                var geofenceList = consignees.Select(c => new GeofenceViewModel
                {
                    FenceID = c.CNEE_CODE,     
                    FenceName = c.CNEE_NAME, 
                    Category = c.SUB_CODE,
                    Address = c.ADDRESS,  
                    City = c.CITY,
                    AddressDetail = $"{c.CNEE_ADDR2} {c.CNEE_ADDR3} {c.CNEE_ADDR4}",
                    PhoneNo = c.CNEE_TEL,
                    ContactName = c.CNEE_PIC,
                    CustomerName = c.CNEE_NAME,
                    MCEasyCustId = c.MCEASY_CUST_ID,
                    GeofenceId = c.MCEASY_GEOFENCE_ID?.ToString(),
                    CreatedOn = c.ENTRY_DATE?.ToString("yyyy-MM-dd HH:mm:ss"),
                    CompanyId = c.SUB_CODE,
                    CUST_GROUP_CODE = c.SUB_CODE,
                    AreaGroup = c.AREA,
                    

                    // sisanya default / null (karena ga ada di table Consignee)
                    Province = null,
                    PostalCode = null,
                    Coordinates = null,
                    Radius = null,
                    ServiceStart = null,
                    ServiceEnd = null,
                    BreakStart = null,
                    BreakEnd = null,
                    ServiceLocType = null,
                    //IsGarage = null,
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
                    message = "Data dari Consignee",
                    data = geofenceList
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

            if (!String.IsNullOrEmpty(category) && id != null) {
                if (category == "consignee") {
                    var consignee = _context.Geofences.FirstOrDefault(o=> o.GeofenceId == id);
                    if (consignee == null)
                    {
                        return View("Form", model);
                    }
                    else {
                        model.ID = consignee.GeofenceId;
                        model.GeofenceId = consignee.GeofenceId.ToString();
                        model.FenceID = consignee.FenceName;
                        model.FenceName = consignee.FenceName;
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
                    }
                }
            }

            return View("Form", model);
        }


        [HttpPost]
        public async Task<IActionResult> CreateGeofence([FromBody] GeofenceViewModel model)
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
                    .FirstOrDefaultAsync(c => c.GeofenceId == model.ID);

                if (ng == null)
                {
                    return NotFound(new { message = "Consignee not found" });
                }

               

                // Kalau pakai API external juga update
                if (customer.API_FLAG == 1 && ng.GeofenceId > 0)
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
            }
            else
            {

                // CREATE
                var ng = new Models.GeofenceTable{};

                if (customer.API_FLAG == 1)
                {
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
                         .Deserialize<Geofence>() ?? new  Geofence();

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

                _context.Geofences.Add(ng);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = model.ID > 0 ? "Consignee Updated Successfully" : "Consignee Created Successfully"
            });
        }
    }

    public class GeofenceViewModel
    {
        public int? ID { get; set; }
        
        public string? FenceID { get; set; }
        [Required]
        public string? FenceName { get; set; }

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