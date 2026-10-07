using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Services.Reporting;
using TMSBilling.Services.Reports;

namespace TMSBilling.Controllers
{
    public class ReportingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly SelectListService _selectList;
        private readonly IEnumerable<IReportGenerator> _reportGenerators;
        private readonly IReportingTokenService _reportingTokenService;

        public ReportingController(
            AppDbContext context,
            SelectListService selectList,
            IEnumerable<IReportGenerator> reportGenerators,
            IReportingTokenService reportingTokenService)
        {
            _context = context;
            _selectList = selectList;
            _reportGenerators = reportGenerators;
            _reportingTokenService = reportingTokenService;
        }

        // ============================================================
        // REPORTING INDEX
        // ============================================================

        [SessionAuthorize]
        public IActionResult Index()
        {
            var config = _context.Configs
                .Where(e => e.key == "reporting")
                .FirstOrDefault();

            if (config == null)
            {
                ViewBag.ErrorMessage =
                    "Configuration not found. Please set up the configuration first.";

                return View("Error");
            }

            var reportingUrl = config.value;

            if (string.IsNullOrWhiteSpace(reportingUrl))
            {
                ViewBag.ErrorMessage =
                    "Reporting URL is not configured.";

                return View("Error");
            }

            // ========================================================
            // GET CURRENT USER
            // ========================================================

            var username =
                User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(username))
            {
                username = "unknown";
            }

            // ========================================================
            // CREATE SHORT-LIVED TOKEN
            // ========================================================

            var token =
                _reportingTokenService.CreateToken(username);

            // ========================================================
            // BUILD IFRAME URL
            // ========================================================

            var separator =
                reportingUrl.Contains("?")
                    ? "&"
                    : "?";

            var iframeSrc =
                $"{reportingUrl}{separator}token={Uri.EscapeDataString(token)}";

            ViewBag.IframeSrc = iframeSrc;

            return View();
        }

        // ============================================================
        // CUSTOM REPORT
        // ============================================================

        [SessionAuthorize]
        public IActionResult Custom()
        {
            var customers = _context.CustomerMains
                .Where(c => c.STATUS_FLAG == 1)
                .OrderBy(c => c.CUST_NAME)
                .ToList();

            return View("Custom", customers);
        }

        // ============================================================
        // DOWNLOAD CUSTOM REPORT
        // ============================================================

        [SessionAuthorize]
        [HttpPost]
        public IActionResult DownloadCustomReport(
            string reportType,
            int customerId,
            DateTime dateFrom,
            DateTime dateTo)
        {
            var generator =
                _reportGenerators
                    .FirstOrDefault(
                        r => r.ReportType == reportType
                    );

            if (generator == null)
            {
                return BadRequest(
                    $"Report type '{reportType}' tidak dikenali."
                );
            }

            var customerName =
                _context.CustomerMains
                    .Where(c => c.ID == customerId)
                    .Select(c => c.MAIN_CUST)
                    .FirstOrDefault()
                ?? $"Customer_{customerId}";

            var bytes =
                generator.Generate(
                    customerId,
                    customerName,
                    dateFrom,
                    dateTo
                );

            var safeCustomer =
                customerName.Replace(" ", "_");

            var fileName =
                $"{reportType}_{safeCustomer}_{dateFrom:yyyyMMdd}_{dateTo:yyyyMMdd}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}