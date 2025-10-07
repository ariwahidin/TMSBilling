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
        //private readonly HttpClient _httpClient;
        //private readonly ApiSettings _apiSettings;
        private readonly SelectListService _selectList;
        private readonly ApiService _apiService;

        public GeofenceController(ILogger<GeofenceController> logger,
            //HttpClient httpClient, 
            //IOptions<ApiSettings> apiSettings, 
            ApiService apiService,
            AppDbContext context,
            SelectListService selectList)
        {
            _logger = logger;
            //_httpClient = httpClient;
            //_apiSettings = apiSettings.Value;
            _apiService = apiService;
            _context = context;
            _selectList = selectList;
            // _geofenceService = geofenceService;
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

                var consignees = await _context.Consignees
                .Where(c => c.MCEASY_GEOFENCE_ID != null)
                .ToListAsync();

                var geofenceList = consignees.Select(c => new GeofenceViewModel
                {
                    FenceID = c.CNEE_CODE,
                    FenceName = c.CNEE_NAME,
                    Category = c.CATEGORY,
                    Customer = c.SUB_CODE,
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
                    Province = c.PROVINCE,
                    PostalCode = c.POSTAL_CODE,
                    


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

            if (!String.IsNullOrEmpty(category) && id != null) {
                if (category == "consignee") {
                    var consignee = _context.Consignees.FirstOrDefault(o=> o.ID == id);
                    if (consignee == null)
                    {
                        return View("Form", model);
                    }
                    else {
                        model.ID = consignee.ID;
                        model.FenceID = consignee.CNEE_CODE;
                        model.FenceName = consignee.CNEE_NAME;
                        model.CUST_GROUP_CODE = consignee.SUB_CODE;
                        model.AreaGroup = consignee.AREA;
                        model.Address = consignee.ADDRESS;
                        model.PostalCode = consignee.POSTAL_CODE;
                        model.Coordinates = consignee.CORDINATES;
                        model.Radius = consignee.RADIUS;
                        model.ContactName = consignee.CNEE_PIC;
                        model.Province = consignee.PROVINCE;
                        model.PhoneNo = consignee.CNEE_TEL;
                        model.City = consignee.CITY;
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
                var existingConsignee = await _context.Consignees
                    .FirstOrDefaultAsync(c => c.ID == model.ID);

                if (existingConsignee == null)
                {
                    return NotFound(new { message = "Consignee not found" });
                }

               

                // Kalau pakai API external juga update
                if (customer.API_FLAG == 1 && existingConsignee.MCEASY_GEOFENCE_ID > 0)
                {

                    var graphqlVariables = new
                    {
                        input = new
                        {
                            geofenceId = existingConsignee.MCEASY_GEOFENCE_ID,
                            oldData = new
                            {
                                fenceName = existingConsignee.CNEE_NAME,
                                city = existingConsignee.CITY,
                                address = existingConsignee.ADDRESS,
                                postalCode = existingConsignee.POSTAL_CODE,
                                province = existingConsignee.PROVINCE,
                                category = existingConsignee.CATEGORY,
                                contactName = existingConsignee.CNEE_PIC,
                                phoneNo = existingConsignee.CNEE_TEL,
                                circData = $"<{existingConsignee.CORDINATES},{existingConsignee.RADIUS}>",
                                isGarage = model.IsGarage,
                                //polyData = existingConsignee.POLYDATA,
                                //isDepot = existingConsignee.IS_DEPOT == 1,
                                //isBillingAddr = existingConsignee.IS_BILLING_ADDR == 1
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
                                //polyData = string.IsNullOrEmpty(model.PolyData) ? null : model.PolyData,
                                //isDepot = model.IsDepot == "true",
                                //isBillingAddr = model.IsBillingAddr == "true"
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
                                    fenceName
                                    city
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

                    // Update fields
                    //existingConsignee.CNEE_CODE = model.FenceID;
                    existingConsignee.CNEE_NAME = model.FenceName;
                    //existingConsignee.MCEASY_CUST_ID = model.MCEasyCustId;
                    existingConsignee.CNEE_ADDR1 = model.City;
                    existingConsignee.CNEE_ADDR2 = model.City;
                    existingConsignee.SUB_CODE = groupCustomer;
                    existingConsignee.CNEE_PIC = model.ContactName;
                    existingConsignee.CNEE_TEL = model.PhoneNo;
                    existingConsignee.CITY = model.City;
                    existingConsignee.CATEGORY = model.Category;
                    existingConsignee.POSTAL_CODE = model.PostalCode;
                    existingConsignee.ADDRESS = model.Address;
                    existingConsignee.AREA = model.AreaGroup;
                    existingConsignee.CORDINATES = model.Coordinates;
                    existingConsignee.RADIUS = model.Radius;
                    existingConsignee.PROVINCE = model.Province;
                    existingConsignee.UPDATE_DATE = date;
                    existingConsignee.UPDATE_USER = user;
                }
            }
            else
            {
                // CREATE
                var newConsignee = new Models.Consignee
                {
                    CNEE_CODE = model.FenceID,
                    CNEE_NAME = model.FenceName,
                    MCEASY_CUST_ID = model.MCEasyCustId,
                    CNEE_ADDR1 = model.City,
                    CNEE_ADDR2 = model.City,
                    ACTIVE_FLAG = 1,
                    SUB_CODE = groupCustomer,
                    CNEE_PIC = model.ContactName,
                    CNEE_TEL = model.PhoneNo,
                    ENTRY_DATE = date,
                    ENTRY_USER = user,
                    CITY = model.City,
                    CATEGORY = model.Category,
                    POSTAL_CODE = model.PostalCode,
                    ADDRESS = model.Address,
                    AREA = model.AreaGroup,
                    CORDINATES = model.Coordinates,
                    RADIUS = model.Radius,
                    PROVINCE = model.Province,
                    IS_GARAGE = model.IsGarage,
                };

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
                                    fenceName
                                    city
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

                    var data = result.GetProperty("data")
                         .GetProperty("createGeofence")
                         .GetProperty("geofence");

                    var geofenceId = data.TryGetProperty("geofenceId", out var idProp) ? idProp.GetInt32() : 0;
                    newConsignee.MCEASY_GEOFENCE_ID = geofenceId;
                    newConsignee.MCEASY_CUST_ID = customerGroup.MCEASY_CUST_ID;
                }

                _context.Consignees.Add(newConsignee);
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
        [Required]
        public string? FenceID { get; set; }
        [Required]
        public string? FenceName { get; set; }

        public string? Customer {  get; set; }

        [Required]
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