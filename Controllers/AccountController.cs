using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Models;

namespace TMSBilling.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login([FromForm] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Form tidak valid." });
            }

            var user = _context.Users.FirstOrDefault(u => u.Username == model.Username);
            if (user != null)
            {
                var hasher = new PasswordHasher<User>();
                var result = hasher.VerifyHashedPassword(user, user.Password, model.Password);

                if (result == PasswordVerificationResult.Success)
                {
                    HttpContext.Session.SetString("username", user.Username);
                    return Json(new { success = true, redirectUrl = "/User/Index" });
                }
            }

            return Json(new { success = false, message = "Username or password invalid!" });
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
