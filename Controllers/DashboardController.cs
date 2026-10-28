using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;

namespace TMSBilling.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(AppDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("User attempted to access dashboard without a valid session.");
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Username = username;
            ViewBag.CurrentDate = DateTime.Now.ToString("dddd, dd MMM yyyy");
            ViewBag.CurrentTime = DateTime.Now.ToString("HH:mm:ss");

            // Counts only pending requests; tariff data is not modified by these queries.
            ViewBag.PriceSellPending = await _context.PriceSellApprovals
                .AsNoTracking()
                .CountAsync(x => x.Status == "PENDING", cancellationToken);

            ViewBag.PriceBuyPending = await _context.PriceBuyApprovals
                .AsNoTracking()
                .CountAsync(x => x.Status == "PENDING", cancellationToken);

            _logger.LogInformation("User {Username} accessed dashboard.", username);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData(CancellationToken cancellationToken)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized(new { success = false, message = "Session expired. Please sign in again." });
            }

            var priceSellPending = await _context.PriceSellApprovals
                .AsNoTracking()
                .CountAsync(x => x.Status == "PENDING", cancellationToken);
            var priceBuyPending = await _context.PriceBuyApprovals
                .AsNoTracking()
                .CountAsync(x => x.Status == "PENDING", cancellationToken);

            return Json(new
            {
                success = true,
                username,
                currentDate = DateTime.Now.ToString("dddd, dd MMM yyyy"),
                currentTime = DateTime.Now.ToString("HH:mm:ss"),
                systemStatus = "Online",
                priceSellPending,
                priceBuyPending,
                totalPendingApprovals = priceSellPending + priceBuyPending,
                message = $"Welcome back, {username}!"
            });
        }
    }
}
