using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Models;

namespace TMSBilling.Controllers
{
    public class CustomerMainController : Controller
    {
        private readonly AppDbContext _context;
        public CustomerMainController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {

            var customers = _context.CustomerMains.ToList(); // pastikan tidak null
            return View(customers); // <-- penting!

        }

        public IActionResult Form(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return PartialView("_Form", new CustomerMain());

            var customer = _context.CustomerMains.FirstOrDefault(c => c.MAIN_CUST == id);
            if (customer == null)
                return NotFound();

            return PartialView("_Form", customer);
        }

        [HttpPost]
        public IActionResult Form(CustomerMain model)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var existing = _context.CustomerMains.Find(model.MAIN_CUST);
            if (existing == null)
            {
                model.ENTRY_DATE = DateTime.Now;
                model.ENTRY_USER = "admin";
                _context.CustomerMains.Add(model);
            }
            else
            {
                existing.CUST_NAME = model.CUST_NAME;
                existing.CUST_CITY = model.CUST_CITY;
                existing.CUST_TEL = model.CUST_TEL;
                existing.STATUS_FLAG = model.STATUS_FLAG;
                existing.UPDATE_DATE = DateTime.Now;
                existing.UPDATE_USER = "admin";
            }

            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        public IActionResult Delete(string id)
        {
            var customer = _context.CustomerMains.Find(id);
            if (customer == null)
                return NotFound();

            _context.CustomerMains.Remove(customer);
            _context.SaveChanges();

            return Ok();
        }


    }
}
