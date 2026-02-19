using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Services.Reports;

namespace TMSBilling.Controllers
{
    public class ReportingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly SelectListService _selectList;
        private readonly IEnumerable<IReportGenerator> _reportGenerators;

        public ReportingController(
            AppDbContext context,
            SelectListService selectList,
            IEnumerable<IReportGenerator> reportGenerators)
        {
            _context = context;
            _selectList = selectList;
            _reportGenerators = reportGenerators;
        }

        public IActionResult Index()
        {
            var config = _context.Configs
                .Where(e => e.key == "reporting")
                .FirstOrDefault();

            if (config == null)
            {
                ViewBag.ErrorMessage = "Configuration not found. Please set up the configuration first.";
                return View("Error");
            }

            ViewBag.Config = config;
            return View();
        }

        [SessionAuthorize]
        public IActionResult Custom()
        {
            var customers = _context.CustomerMains
                .Where(c => c.STATUS_FLAG == 1)
                .OrderBy(c => c.CUST_NAME)
                .ToList();

            return View("Custom", customers);
        }

        [SessionAuthorize]
        [HttpPost]
        public IActionResult DownloadCustomReport(string reportType, int customerId, DateTime dateFrom, DateTime dateTo)
        {
            // Cari generator yang sesuai dengan reportType
            var generator = _reportGenerators.FirstOrDefault(r => r.ReportType == reportType);
            if (generator == null)
                return BadRequest($"Report type '{reportType}' tidak dikenali.");

            var customerName = _context.CustomerMains
                .Where(c => c.ID == customerId)
                .Select(c => c.MAIN_CUST)
                .FirstOrDefault() ?? $"Customer_{customerId}";

            var bytes = generator.Generate(customerId, customerName, dateFrom, dateTo);

            var safeCustomer = customerName.Replace(" ", "_");
            var fileName = $"{reportType}_{safeCustomer}_{dateFrom:yyyyMMdd}_{dateTo:yyyyMMdd}.xlsx";

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}