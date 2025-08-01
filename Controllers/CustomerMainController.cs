using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;


namespace TMSBilling.Controllers
{
    [SessionAuthorize]
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

        public IActionResult Form(int? id)
        {
            if (id == null)
                return PartialView("_Form", new CustomerMain());

            var customer = _context.CustomerMains.FirstOrDefault(c => c.ID == id);
            if (customer == null)
                return NotFound();

            return PartialView("_Form", customer);
        }


        [HttpPost]
        public IActionResult Form(CustomerMain model)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var existing = _context.CustomerMains.Find(model.ID);

            if (existing == null)
            {

                bool exists = _context.CustomerMains.Any(v => v.MAIN_CUST == model.MAIN_CUST);
                if (exists)
                {
                    return BadRequest(new
                    {
                        message = "Main customer already exists"
                    });
                }

                model.ENTRY_DATE = DateTime.Now;
                model.ENTRY_USER = HttpContext.Session.GetString("username") ?? "System";
                _context.CustomerMains.Add(model);
            }
            else
            {

                bool duplicate = _context.CustomerMains.Any(v => v.MAIN_CUST == model.MAIN_CUST && v.ID != model.ID);
                if (duplicate)
                {
                    return BadRequest(new
                    {
                        message = "Main customer already exists on another record"
                    });
                }

                existing.CUST_NAME = model.CUST_NAME;
                existing.CUST_CITY = model.CUST_CITY;
                existing.CUST_TEL = model.CUST_TEL;
                existing.STATUS_FLAG = model.STATUS_FLAG;
                existing.UPDATE_DATE = DateTime.Now;
                existing.UPDATE_USER = HttpContext.Session.GetString("username") ?? "System";
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
