using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;

namespace TMSBilling.Controllers
{
    public class ReportingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly SelectListService _selectList;

        public ReportingController(AppDbContext context, SelectListService selectList)
        {
            _context = context;
            _selectList = selectList;
        }


        public IActionResult Index()
        {
            var config  = _context.Configs.FirstOrDefault();

            if (config == null)
            {
                ViewBag.ErrorMessage = "Configuration not found. Please set up the configuration first.";
                return View("Error");
            }

            ViewBag.Config = config; // lempar config ke view
            return View();
        }
    }
}
