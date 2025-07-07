using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Models.ViewModels;

namespace TMSBilling.Controllers
{

    [SessionAuthorize]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var orders = _context.Orders
                .OrderByDescending(o => o.entry_date)
                .Take(100)
                .ToList();

            return View(orders); // kirim list ke view
        }

        public IActionResult Form(int? id)
        {
            var vm = new OrderViewModel();

            if (id.HasValue)
            {
                vm.Header = _context.Orders.FirstOrDefault(o => o.id_seq == id.Value) ?? new Order();
                vm.Details = _context.OrderDetails
                    .Where(d => d.id_seq_order == id.Value)
                    .ToList();
            }

            return View(vm);
        }

        [HttpPost]
        public IActionResult Save([FromBody] OrderViewModel model)
        {
            if (model == null || model.Header == null || model.Details == null)
            {
                return BadRequest(new { success = false, message = "Data tidak valid." });
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var header = model.Header;

                if (header.id_seq == 0)
                {
                    // INSERT (create)
                    header.entry_date = DateTime.Now;
                    header.entry_user = HttpContext.Session.GetString("username") ?? "System";
                    _context.Orders.Add(header);
                    _context.SaveChanges();
                }
                else
                {
                    // UPDATE
                    var existingHeader = _context.Orders.FirstOrDefault(o => o.id_seq == header.id_seq);
                    if (existingHeader == null)
                        return NotFound(new { success = false, message = "Data tidak ditemukan." });

                    existingHeader.wh_code = header.wh_code;
                    existingHeader.sub_custid = header.sub_custid;
                    existingHeader.cnee_code = header.cnee_code;
                    existingHeader.inv_no = header.inv_no;
                    existingHeader.delivery_date = header.delivery_date;
                    existingHeader.dest_area = header.dest_area;
                    existingHeader.truck_size = header.truck_size;
                    existingHeader.moda_req = header.moda_req;
                    existingHeader.serv_req = header.serv_req;
                    existingHeader.order_status = header.order_status;
                    existingHeader.update_date = DateTime.Now;
                    existingHeader.update_user = HttpContext.Session.GetString("username") ?? "System";

                    _context.Orders.Update(existingHeader);

                    // Hapus detail lama dulu
                    var existingDetails = _context.OrderDetails.Where(d => d.id_seq_order == header.id_seq).ToList();
                    _context.OrderDetails.RemoveRange(existingDetails);
                    _context.SaveChanges();
                }

                // Simpan detail
                foreach (var detail in model.Details)
                {
                    detail.id_seq = 0; // pastikan tidak isi id_seq
                    detail.id_seq_order = header.id_seq;
                    detail.entry_date = DateTime.Now;
                    detail.entry_user = HttpContext.Session.GetString("username") ?? "System";
                    detail.update_date= DateTime.Now;
                    detail.update_user = HttpContext.Session.GetString("username") ?? "System";
                    _context.OrderDetails.Add(detail);
                }

                _context.SaveChanges();
                transaction.Commit();

                return Json(new { success = true, message = "Data berhasil disimpan!" });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                transaction.Rollback();
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan: " + message });
            }
        }
    }
}
