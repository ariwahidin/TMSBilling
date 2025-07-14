using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Models;
using TMSBilling.Models.ViewModels;

namespace TMSBilling.Controllers
{
    public class JobController : Controller
    {

        private readonly AppDbContext _context;
        private readonly SelectListService _selectList;
        public JobController(AppDbContext context, SelectListService selectList)
        {
            _context = context;
            _selectList = selectList;
        }

        public IActionResult Index()
        {
            var jobs = _context.Jobs
                .OrderByDescending(j => j.entry_date)
                .Take(100)
                .ToList();

            return View(jobs);
        }

        public IActionResult Form(int? id)
        {
            var vm = new JobViewModel();
            ViewBag.ListCustomer = _selectList.getCustomers();
            ViewBag.ListWarehouse = _selectList.GetWarehouse();
            ViewBag.ListConsignee = _selectList.GetConsignee();
            ViewBag.ListOrigin = _selectList.GetOrigins();
            ViewBag.ListDestination = _selectList.GetDestinations();
            ViewBag.ListUoM = _selectList.GetChargeUoms();
            ViewBag.ListModa = _selectList.GetServiceModas();
            ViewBag.ListServiceType = _selectList.GetServiceTypes();
            ViewBag.ListTruckSize = _selectList.GetTruckSizes();

            if (id.HasValue)
            {
                vm.Header = _context.Jobs.FirstOrDefault(o => o.id_seq == id.Value) ?? new Job();
                //vm.Details = _context.OrderDetails
                //    .Where(d => d.id_seq_order == id.Value)
                //    .ToList();
            }

            return View(vm);

            //return View();
        }
    }
}
