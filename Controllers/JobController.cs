using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Models.ViewModels;
using static TMSBilling.Models.ViewModels.JobViewModel;

namespace TMSBilling.Controllers
{

    [SessionAuthorize]
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
                .ToList()
                .GroupBy(j => j.jobid)
                .Select(g => new JobListViewModel
                {
                    JobId = g.Key,
                    Origin = g.FirstOrDefault()?.origin_id,
                    Destination = g.FirstOrDefault()?.dest_id,
                    DeliveryDate = g.FirstOrDefault()?.dvdate,
                    Vendor = g.FirstOrDefault()?.vendorid,
                    TruckID = g.FirstOrDefault()?.truckid,
                }).ToList();

            return View(jobs);
        }


        [Route("Job/Form/{jobid?}")]
        public IActionResult Form(string? jobid)
        {
            var vm = new JobViewModel();

            // ViewBag dropdown
            ViewBag.ListCustomer = _selectList.getCustomers();
            ViewBag.ListWarehouse = _selectList.GetWarehouse();
            ViewBag.ListConsignee = _selectList.GetConsignee();
            ViewBag.ListOrigin = _selectList.GetOrigins();
            ViewBag.ListDestination = _selectList.GetDestinations();
            ViewBag.ListUoM = _selectList.GetChargeUoms();
            ViewBag.ListModa = _selectList.GetServiceModas();
            ViewBag.ListServiceType = _selectList.GetServiceTypes();
            ViewBag.ListTruckSize = _selectList.GetTruckSizes();
            ViewBag.ListPickupCargo = _selectList.GetYesNo();
            ViewBag.ListMultiDrop = _selectList.GetYesNo();
            ViewBag.ListMultitrip = _selectList.GetYesNo();

            if (!string.IsNullOrEmpty(jobid))
            {
                var jobRows = _context.Jobs
                    .Where(o => o.jobid == jobid)
                    .OrderBy(o => o.drop_seq)
                    .ToList();

                if (jobRows.Any())
                {
                    vm.Header = jobRows.First();   // Ambil salah satu sbg header
                    //vm.Details = jobRows;          // Semua jadi detail
                }
            }

            return View(vm);
        }


        [HttpGet]
        public IActionResult GetOrdersByDate(string date)
        {
            if (string.IsNullOrEmpty(date))
                return BadRequest(new { success = false, message = "Tanggal tidak valid" });

            try
            {
                DateTime parsedDate = DateTime.ParseExact(date, "yyyy-MM-dd", null);

                var orders = _context.Orders
                    .Where(o => o.delivery_date == parsedDate)
                    .Select(o => new
                    {
                        o.id_seq,
                        o.wh_code,
                        o.sub_custid,
                        o.inv_no,
                        o.delivery_date,
                        o.origin_id,
                        o.dest_area,
                        o.tot_pkgs,
                        o.truck_size,
                        o.order_status,
                        o.remark
                    })
                .ToList();

                return Ok(new { success = true, data = orders });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public IActionResult Save([FromBody] List<Job> jobList)
        {

            // Bagian Validasi






            // Bagian Eksekusi
            // Buat prefix bulanan: TRYP2507
            string monthPrefix = "TRYP" + DateTime.Now.ToString("yyMM");

            // Hitung jumlah jobid yang sudah ada untuk bulan ini
            int existingCount = _context.JobHeaders
                .Where(j => j.jobid != null && j.jobid.StartsWith(monthPrefix))
                .Count();

            // Buat jobid baru
            string newJobId = GenerateJobId(existingCount + 1);

            var jobHeader = new JobHeader();
            jobHeader.jobid = newJobId;
            _context.JobHeaders.Add(jobHeader);

            foreach (var job in jobList)
            {
                job.jobid = newJobId;
                _context.Jobs.Add(job);
            }
            _context.SaveChanges();
            return Json(new { success = true, message = "Job berhasil disimpan" });
        }

        private string GenerateJobId(int sequence)
        {
            string prefix = "TRYP";
            string year = DateTime.Now.ToString("yy");  // contoh: 25
            string month = DateTime.Now.ToString("MM"); // contoh: 07
            string sequencePart = sequence.ToString("D4"); // 0001, 0002, dst

            return $"{prefix}{year}{month}{sequencePart}";
        }

        [HttpGet]
        public IActionResult GetVendors(string originId, string destId, string truckSize)
        {
            var vendors = _context.PriceBuys
                .Where(v =>
                    v.active_flag == 1 &&
                    v.origin == originId &&
                    v.dest == destId &&
                    v.truck_size == truckSize
                )
                .OrderBy(v => v.buy1)
                .Select(v => new {
                    SupCode = v.sup_code,
                    SupName = v.sup_code
                })
                .Distinct()
                .ToList();

            if (vendors.Any())
            {
                return Json(new { success = true, vendors });
            }

            return Json(new { success = false, vendors = new List<object>() });
        }

        [HttpGet]
        public IActionResult GetDriversByVendor(string supCode)
        {
            var drivers = _context.VendorTrucks
                .Where(v =>
                    v.vehicle_active == 1 &&
                    v.sup_code == supCode
                )
                //.OrderBy(v => v.buy1)
                .Select(v => new {
                    DriverName = v.vehicle_driver,
                    TruckId = v.vehicle_no
                })
                .Distinct()
                .ToList();

            if (drivers.Any())
            {
                return Json(new { success = true, drivers });
            }

            return Json(new { success = false, drivers = new List<object>() });
        }

        [HttpGet]
        public IActionResult GetJobDetails(string jobid)
        {
            var details = _context.Jobs
                .Where(j => j.jobid == jobid)
                .ToList();

            return Json(new { success = true, data = details });
        }
    }
}
