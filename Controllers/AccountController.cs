using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Models.ViewModels;

namespace TMSBilling.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<AccountController> _logger;


        public AccountController(
            AppDbContext context,
            IPasswordHasher<User> passwordHasher,
            ILogger<AccountController> logger
            )
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
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
                    HttpContext.Session.SetString("is_admin", user.IsAdmin ? "true" : "false");

                    return Json(new { success = true, redirectUrl = "/Dashboard/Index" });
                }
            }

            return Json(new { success = false, message = "Username atau password salah!" });
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [SessionAuthorize]
        public IActionResult ChangePassword()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Get current username from session
                var username = HttpContext.Session.GetString("username");
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Username not found in session when attempting to change password");
                    ViewBag.Error = "User session expired. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                // Get user from database by username
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    _logger.LogWarning($"User not found in database with username: {username}");
                    ViewBag.Error = "User not found. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                // Verify current password
                var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, model.CurrentPassword);
                if (verificationResult == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                    ViewBag.Error = "Current password is incorrect.";
                    return View(model);
                }

                // Hash new password and update
                user.Password = _passwordHasher.HashPassword(user, model.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {user.Id} changed password successfully");

                ViewBag.Message = "Your password has been changed successfully.";

                // Clear the form
                ModelState.Clear();
                return View(new ChangePasswordViewModel { CurrentPassword = string.Empty, ConfirmPassword = string.Empty, NewPassword = string.Empty } );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while changing password");
                ViewBag.Error = "An error occurred while changing your password. Please try again.";
                return View(model);
            }
        }

        // API endpoint for AJAX calls
        [HttpPost]
        public async Task<IActionResult> ChangePasswordApi([FromBody] ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid input data", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            try
            {
                // Get current username from session
                var username = HttpContext.Session.GetString("username");
                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "User session expired. Please log in again." });
                }

                // Get user from database by username
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found. Please log in again." });
                }

                // Verify current password
                var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, model.CurrentPassword);
                if (verificationResult == PasswordVerificationResult.Failed)
                {
                    return Json(new { success = false, message = "Current password is incorrect." });
                }

                // Hash new password and update
                user.Password = _passwordHasher.HashPassword(user, model.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {user.Id} changed password successfully via API");

                return Json(new { success = true, message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while changing password via API");
                return Json(new { success = false, message = "An error occurred while changing your password." });
            }
        }

    }
}
