using Microsoft.AspNetCore.Mvc;
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


namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class GeofenceController : Controller
    {
        private readonly ILogger<GeofenceController> _logger;
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;

        public GeofenceController(ILogger<GeofenceController> logger, HttpClient httpClient, IOptions<ApiSettings> apiSettings, AppDbContext context)
        {
            _logger = logger;
            _httpClient = httpClient;
            _apiSettings = apiSettings.Value;
            _context = context;
            // _geofenceService = geofenceService;
        }

        // GET: /Geofence
        public async Task<IActionResult> Index(Guid? id)
        {
            Console.WriteLine($"Customer ID from query: {id}");

            if (id == null)
            {
                return BadRequest("Id tidak boleh kosong");
            }



            ViewBag.CustomerId = id.Value;

            var gqlRequest = new
            {
                operationName = "Geofences",
                query = @"query Geofences($filter: GetGeofencesFilter) {
                geofences(filter: $filter) {
                    status
                    isSuccessful
                    geofences {
                        data {
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
                            hasRelation
                        }
                    }
                }
            }",
                variables = new
                {
                    filter = new
                    {
                        customerId = id.Value,
                        isCustomer = true,
                        pagination = new
                        {
                            page = 1,
                            show = 10
                        }
                    }
                }
            };

            var token = _apiSettings.Token;
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring("Bearer ".Length);
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNameCaseInsensitive = true // Agar properti JSON tidak sensitif huruf besar/kecil
            };

            // Kirim permintaan GraphQL
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiSettings.BaseUrlGraphql}",
                gqlRequest,
                options
            );

            var responseText = await response.Content.ReadAsStringAsync();

            // Debug: Cetak respons untuk melihat detailnya
            Console.WriteLine("=== GRAPHQL RESPONSE GEOFENCES ===");
            Console.WriteLine(responseText);

            // Jika status HTTP bukan 200 OK
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Gagal kirim ke API GRAPHQL. Status HTTP: {response.StatusCode}",
                    detail = responseText
                });
            }

            var json = JObject.Parse(responseText);

            // Ambil list geofences
            var geofences = json["data"]?["geofences"]?["geofences"]?["data"]?.ToObject<List<Geofence>>();

            var customer = await _context.CustomerGroups
                .Where(c => c.MCEASY_CUST_ID == id.ToString())
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { message = "Customer Group tidak ditemukan" });
            }

            ViewBag.CustomerName = customer.SUB_CODE; // <- ganti sesuai field yg ada

            return View(geofences);


            //try
            //{
            //    return View();
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error occurred while loading geofence index");
            //    TempData["Error"] = "Terjadi kesalahan saat memuat data geofence.";
            //    return View();
            //}
        }

        // GET: /Geofence/Create
        public async Task<IActionResult> Create(Guid? id)
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
            var model = new GeofenceViewModel();
            return View("Form", model);
        }


        [HttpPost]
        public async Task<IActionResult> CreateGeofence([FromBody] GeofenceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = await _context.CustomerGroups
                .Where(c => c.MCEASY_CUST_ID == model.CustomerId)
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { message = "Customer Group tidak ditemukan" });
            }

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
                    serviceStart = string.IsNullOrEmpty(model.ServiceStart) ? null : model.ServiceStart,
                    serviceEnd = string.IsNullOrEmpty(model.ServiceEnd) ? null : model.ServiceEnd,
                    breakStart = string.IsNullOrEmpty(model.BreakStart) ? null : model.BreakStart,
                    breakEnd = string.IsNullOrEmpty(model.BreakEnd) ? null : model.BreakEnd,
                    isDepot = model.IsDepot == "true",
                    isBillingAddr = model.IsBillingAddr == "true",
                    customerName = customer.SUB_CODE,
                    customerId = model.CustomerId // CustomerId sudah dijamin tidak kosong
                }
            };

            // GraphQL request body
            var gqlRequest = new
            {
                operationName = "CreateGeofence",
                query = @"mutation CreateGeofence($input: CreateGeofenceInput!) { 
                    createGeofence(input: $input) { 
                        isSuccessful 
                        message 
                        geofence
                            { 
                                geofenceId 
                                fenceName 
                                city
                            } 
                        }
                    }",
                variables = graphqlVariables
            };

            var token = _apiSettings.Token;
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring("Bearer ".Length);
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNameCaseInsensitive = true // Agar properti JSON tidak sensitif huruf besar/kecil
            };

            // Kirim permintaan GraphQL
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiSettings.BaseUrlGraphql}",
                gqlRequest,
                options
            );

            var responseText = await response.Content.ReadAsStringAsync();

            // Debug: Cetak respons untuk melihat detailnya
            Console.WriteLine("=== GRAPHQL RESPONSE BODY ===");
            Console.WriteLine(responseText);

            // Jika status HTTP bukan 200 OK
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Gagal kirim ke API GRAPHQL. Status HTTP: {response.StatusCode}",
                    detail = responseText
                });
            }

            var json = JObject.Parse(responseText);
            var geofenceId = json["data"]?["createGeofence"]?["geofence"]?["geofenceId"]?.Value<int>() ?? 0;
            var fenceName = json["data"]?["createGeofence"]?["geofence"]?["fenceName"]?.Value<string>() ?? model.FenceName;
            var city = json["data"]?["createGeofence"]?["geofence"]?["city"]?.Value<string>() ?? model.City;
            var address = model.Address;
            var groupCustomer = customer.SUB_CODE;
            var user = HttpContext.Session.GetString("username") ?? "System";
            var date = DateTime.Now;

            var newConsignee = new Models.Consignee
            {
                CNEE_CODE = fenceName,
                CNEE_NAME = fenceName,
                MCEASY_GEOFENCE_ID = geofenceId,
                CNEE_ADDR1 = city,
                CNEE_ADDR2 = city,
                ACTIVE_FLAG = 1,
                SUB_CODE = groupCustomer,
                CNEE_PIC = model.ContactName,
                CNEE_TEL = model.PhoneNo,
                ENTRY_DATE = date,
                ENTRY_USER = user,
            };

            // Insert ke DB
            _context.Consignees.Add(newConsignee);
            await _context.SaveChangesAsync();

            // Jika berhasil
            return Ok(new
            {
                message = "Geofence berhasil dibuat di GraphQL"
            });
        }
    }


    public class GeofenceViewModel
    {
        public string FenceName { get; set; }
        public string Category { get; set; }
        public string Address { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Coordinates { get; set; }
        public string Radius { get; set; }
        public string AddressDetail { get; set; }
        public string ContactName { get; set; }
        public string PhoneNo { get; set; }
        public string CustomerName { get; set; }
        public string ServiceStart { get; set; }
        public string ServiceEnd { get; set; }
        public string BreakStart { get; set; }
        public string BreakEnd { get; set; }
        public string ServiceLocType { get; set; }
        public string IsGarage { get; set; }
        public string IsServiceLoc { get; set; }
        public string IsBillingAddr { get; set; }
        public string IsDepot { get; set; }
        public string IsAlert { get; set; }
        public string GeofenceId { get; set; }
        public string CompanyId { get; set; }
        public string CustomerId { get; set; }
        public string Type { get; set; }
        public string PolyData { get; set; }
        public string CircData { get; set; }
        public string CreatedOn { get; set; }
        public string HasRelation { get; set; }
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
        public string customerId { get; set; }
        public string fenceName { get; set; }
        public string type { get; set; }
        public string polyData { get; set; }
        public string circData { get; set; }
        public string address { get; set; }
        public string addressDetail { get; set; }
        public string province { get; set; }
        public string city { get; set; }
        public string postalCode { get; set; }
        public string category { get; set; }
        public string contactName { get; set; }
        public string phoneNo { get; set; }
        public bool isGarage { get; set; }
        public bool isServiceLoc { get; set; }
        public bool isBillingAddr { get; set; }
        public bool isDepot { get; set; }
        public bool isAlert { get; set; }
        public string serviceStart { get; set; }
        public string serviceEnd { get; set; }
        public string breakStart { get; set; }
        public string breakEnd { get; set; }
        public string serviceLocType { get; set; }
        public string customerName { get; set; }
        public bool hasRelation { get; set; }
    }


    //public class Geofence
    //{
    //    public string? GeofenceId { get; set; }
    //    public string? FenceName { get; set; }
    //}

}