using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(AppDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View("Form", new User()); // kosong
        }

        [HttpGet]
        public IActionResult GetUsers()
        {
            var users = _context.Users
                .OrderByDescending(u => u.Id)
                .Select(u => new {
                u.Id,
                u.Username,
                u.UpdatedAt,
                u.UpdatedBy
            }).ToList();

            return Ok(new
            {
                success = true,
                message = "User data found",
                data = users
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            _context.SaveChanges();

            return Ok(); // 200 success
        }

        [HttpPost]
        public IActionResult CreateAjax([FromBody] User user)
        {

            _logger.LogInformation("Masuk ke CreateAjax. Payload: {@User}", user);
            Console.WriteLine($"Username: {user.Username}, Password: {user.Password}");

            if (!HttpContext.Session.Keys.Contains("username"))
                return RedirectToAction("Login", "Account");


            if (_context.Users.Any(u => u.Username == user.Username))
            {
                ModelState.AddModelError("Username", "Username sudah digunakan.");
            }


            if (ModelState.IsValid)
            {
                var hasher = new PasswordHasher<User>();
                user.Password = hasher.HashPassword(user, user.Password);
                user.CreatedBy = HttpContext.Session.GetString("username"); 
                user.CreatedAt = DateTime.Now;
                user.UpdatedBy = HttpContext.Session.GetString("username");
                user.UpdatedAt = DateTime.Now;
                _context.Users.Add(user);
                _context.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = "User berhasil ditambahkan.",
                    data = user
                });
            }

            return BadRequest(new
            {
                success = false,
                message = "Data tidak valid.",
                errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });
        }

        public IActionResult Edit(int id)
        {
            if (!HttpContext.Session.Keys.Contains("username"))
                return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            return View("Form", user);
        }


        [HttpPost]
        public IActionResult EditAjax([FromBody] User model)
        {

            if (!HttpContext.Session.Keys.Contains("username"))
                return RedirectToAction("Login", "Account");
            Console.WriteLine($"Username edit: {model.Username}");

            var user = _context.Users.Find(model.Id);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User tidak ditemukan." });
            }

            // Validasi: apakah username sudah dipakai oleh user lain
            var existingUser = _context.Users
                .Where(u => u.Username == model.Username && u.Id != model.Id)
                .FirstOrDefault();

            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Username sudah digunakan oleh user lain.");
                return BadRequest(new
                {
                    success = false,
                    message = "Validasi gagal.",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            // Update data
            user.Username = model.Username;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var hasher = new PasswordHasher<User>();
                user.Password = hasher.HashPassword(user, model.Password);
            }

            user.UpdatedBy = HttpContext.Session.GetString("username") ?? "system";
            user.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            return Ok(new
            {
                success = true,
                message = "User berhasil diedit.",
                data = user
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBulk([FromBody] BulkDeleteModel model)
        {

            if (!HttpContext.Session.Keys.Contains("username"))
                return RedirectToAction("Login", "Account");

            if (model?.Ids == null || model.Ids.Count == 0)
                return BadRequest(new { message = "No data selected" });

            var users = _context.Users.Where(u => model.Ids.Contains(u.Id)).ToList();

            if (users.Count == 0)
                return NotFound();

            _context.Users.RemoveRange(users);
            _context.SaveChanges();

            return Ok(new
            {
                succ = true,
                message = "Deleted successfully"
            });

        }

        public class BulkDeleteModel
        {
            public required List<int> Ids { get; set; }
        }
    }
}
