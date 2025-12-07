using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class UserCustomerController : Controller
    {
        private readonly AppDbContext _context;

        public UserCustomerController(AppDbContext context)
        {
            _context = context;
        }

        // =======================
        // LIST DATA
        // =======================
        public IActionResult Index()
        {
            var data = _context.UserXCustomers.ToList();

            return View(data);
        }

        // =======================
        // FORM INPUT
        // =======================
        public IActionResult Create()
        {
            ViewBag.Users = _context.Users.ToList();
            ViewBag.Customers = _context.CustomerMains.ToList();
            return View();
        }

        // =======================
        // SIMPAN
        // =======================
        [HttpPost]
        public IActionResult Create(string Username, List<string> CustomerMains)
        {
            if (string.IsNullOrEmpty(Username))
                return BadRequest("Username kosong");

            var user = _context.Users.FirstOrDefault(u => u.Username == Username);
            if (user == null)
                return NotFound("User tidak ditemukan");

            // 1. Hapus semua relasi lama user ini
            var oldData = _context.UserXCustomers
                .Where(x => x.UserName == Username)
                .ToList();

            _context.UserXCustomers.RemoveRange(oldData);

            // 2. Insert ulang dari checkbox
            if (CustomerMains != null && CustomerMains.Any())
            {
                foreach (var cust in CustomerMains)
                {
                    var data = new UserXCustomer
                    {
                        UserId = user.Id,
                        UserName = Username,
                        CustomerMain = cust,
                        CreatedBy = HttpContext.Session.GetString("username") ?? "System",
                        CreatedAt = DateTime.Now,
                    };

                    _context.UserXCustomers.Add(data);
                }
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }


        // =======================
        // DELETE
        // =======================
        public IActionResult Delete(int id)
        {
            var data = _context.UserXCustomers.Find(id);
            if (data != null)
            {
                _context.UserXCustomers.Remove(data);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }


        [HttpGet]
        public IActionResult GetCustomersByUser(string username)
        {
            var data = _context.UserXCustomers
                .Where(x => x.UserName == username)
                .Select(x => x.CustomerMain)
                .ToList();

            return Json(data);
        }

    }
}
