using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;

namespace TMSBilling.Controllers
{
    public class DashboardController : Controller
    {

        private readonly AppDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            AppDbContext context,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        //public IActionResult Index()
        //{
        //    return View();
        //}

        public IActionResult Index()
        {
            // Check if user is logged in (has session)
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("User attempted to access dashboard without valid session");
                return RedirectToAction("Login", "Account");
            }

            // Pass username to view
            ViewBag.Username = username;

            // You can add more data here later
            ViewBag.CurrentDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
            ViewBag.CurrentTime = DateTime.Now.ToString("HH:mm:ss");

            _logger.LogInformation($"User {username} accessed dashboard");

            return View();
        }

        // Optional: API endpoint to get dashboard data
        [HttpGet]
        public IActionResult GetDashboardData()
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Session expired" });
            }

            var dashboardData = new
            {
                success = true,
                username = username,
                currentDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy"),
                currentTime = DateTime.Now.ToString("HH:mm:ss"),
                systemStatus = "Online",
                message = $"Welcome back, {username}!"
            };

            return Json(dashboardData);
        }
    }
}
