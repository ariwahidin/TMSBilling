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

        public CustomerController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var data = _context.Customers
                .OrderByDescending(c => c.ID)
                .ToList();
            return View(data);
        }

        [HttpGet]
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
                existing.API_FLAG = model.API_FLAG;
                existing.UPDATE_DATE = DateTime.Now;
                existing.UPDATE_USER = HttpContext.Session.GetString("username") ?? "System";
            }
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
