using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;


namespace TMSBilling.Controllers
{

    [SessionAuthorize]
    public class CustomerController : Controller
    {

        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;

        public CustomerController(HttpClient httpClient, IOptions<ApiSettings> apiSettings, AppDbContext context)
        {
            _httpClient = httpClient;
            _apiSettings = apiSettings.Value;
            _context = context;
        }

        public IActionResult Index()
        {
            var data = _context.Customers.ToList();
            return View(data);
        }

        public IActionResult Form(int? id)
        {
            ViewBag.MainCustomerList = _context.CustomerMains
                .Select(m => new SelectListItem
                {
                    Value = m.MAIN_CUST,
                    Text = $"{m.MAIN_CUST} - {m.CUST_NAME}"
                }).ToList();

            var model = id == null ? new Customer() : _context.Customers.Find(id);
            return PartialView("_Form", model);
        }

        [HttpPost]
        public async Task<IActionResult> Form(Customer model)
        {
            if (!ModelState.IsValid)
            {
                foreach (var entry in ModelState)
                {
                    foreach (var err in entry.Value.Errors)
                    {
                        Console.WriteLine($"Field {entry.Key}: {err.ErrorMessage}");
                    }
                }

                BadRequest(new
                {
                    message = "Please fill all input field with correct value"
                });
            }

            var existing = _context.Customers.Find(model.ID);
            if (existing == null)
            {
                //_httpClient.DefaultRequestHeaders.Authorization =
                //        new AuthenticationHeaderValue("Bearer", _apiSettings.Token.Replace("Bearer ", ""));

                //var payload = new
                //{
                //    name = model.CUST_NAME,
                //};

                //var options = new JsonSerializerOptions
                //{
                //    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                //};

                //var jsonPayload = JsonSerializer.Serialize(payload, options);
                //var jsonContent = new StringContent(
                //    jsonPayload,
                //    Encoding.UTF8,
                //    "application/json"
                //);

                //Console.WriteLine("=== DEBUG PAYLOAD DETAIL ===");
                //Console.WriteLine($"customer_name: {payload.name}");
                //Console.WriteLine("=== JSON PAYLOAD ===");
                //Console.WriteLine(jsonPayload);
                //Console.WriteLine("=== HEADERS ===");
                //Console.WriteLine($"Authorization: {_httpClient.DefaultRequestHeaders.Authorization}");
                //Console.WriteLine($"Content-Type: application/json");
                //Console.WriteLine("=====================");

                //var response = await _httpClient.PostAsync(
                //    $"{_apiSettings.BaseUrl}/customer",
                //    jsonContent
                //);

                //var responseText = await response.Content.ReadAsStringAsync();

                //if (!response.IsSuccessStatusCode)
                //{
                //    return BadRequest(new
                //    {
                //        success = false,
                //        message = "Gagal kirim ke API Customer",
                //        detail = responseText
                //    });
                //}

                //using var doc = JsonDocument.Parse(responseText);
                //var id = doc.RootElement.GetProperty("data").GetProperty("id").GetString();
                //model.MCEASY_CUST_ID = id;


                bool exists = _context.Customers.Any(v => v.CUST_CODE == model.CUST_CODE);
                if (exists)
                {
                    return BadRequest(new
                    {
                        message = "Customer ID already exists"
                    });
                }

                model.ENTRY_DATE = DateTime.Now;
                model.ENTRY_USER = HttpContext.Session.GetString("username") ?? "System";
                _context.Customers.Add(model);
            }
            else
            {

                bool duplicate = _context.Customers.Any(v => v.CUST_CODE == model.CUST_CODE && v.ID != model.ID);
                if (duplicate)
                {
                    return BadRequest(new
                    {
                        message = "Customer ID already exists on another record"
                    });
                }

                existing.CUST_CODE = model.CUST_CODE;
                existing.CUST_NAME = model.CUST_NAME;
                existing.CUST_ADDR1 = model.CUST_ADDR1;
                existing.CUST_ADDR2 = model.CUST_ADDR2;
                existing.CUST_CITY = model.CUST_CITY;
                existing.CUST_EMAIL = model.CUST_EMAIL;
                existing.CUST_TEL = model.CUST_TEL;
                existing.CUST_FAX = model.CUST_FAX;
                existing.CUST_PIC = model.CUST_PIC;
                existing.TAX_REG_NO = model.TAX_REG_NO;
                existing.MAIN_CUST = model.MAIN_CUST;
                existing.CUST_CUTOFF = model.CUST_CUTOFF;
                existing.ACTIVE_FLAG = model.ACTIVE_FLAG;
                existing.UPDATE_DATE = DateTime.Now;
                existing.UPDATE_USER = HttpContext.Session.GetString("username") ?? "System";
            }

            //_context.SaveChanges();
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var data = _context.Customers.Find(id);
            if (data == null) return NotFound();

            _context.Customers.Remove(data);
            _context.SaveChanges();
            return Ok();
        }
    }
}
