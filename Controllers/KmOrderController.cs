using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class KmOrderController : Controller
    {
        private readonly AppDbContext _context;

        public KmOrderController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get distinct values for dropdowns
            ViewBag.ServReqList = await _context.Jobs
                .Where(j => !string.IsNullOrEmpty(j.serv_req))
                .Select(j => j.serv_req)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            ViewBag.UomList = await _context.Jobs
                .Where(j => !string.IsNullOrEmpty(j.charge_uom))
                .Select(j => j.charge_uom)
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();

            ViewBag.VendorList = await _context.Jobs
                .Where(j => !string.IsNullOrEmpty(j.vendorid))
                .Select(j => j.vendorid)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetFilteredData(
            DateTime? startDate,
            DateTime? endDate,
            string? servReq,
            string? uom,
            string? vendorId)
        {
            try
            {
                // Start with Jobs and join to Orders
                var query = from j in _context.Jobs
                            join o in _context.Orders on j.inv_no equals o.inv_no
                            select new { Job = j, Order = o };

                // Apply filters
                //if (startDate.HasValue)
                //{
                //    query = query.Where(x => x.Order.delivery_date >= startDate.Value);
                //}

                //if (endDate.HasValue)
                //{
                //    query = query.Where(x => x.Order.delivery_date <= endDate.Value);
                //}

                if (startDate.HasValue && endDate.HasValue)
                {
                    var start = startDate.Value.Date;
                    var end = endDate.Value.Date.AddDays(1).AddTicks(-1);

                    query = query.Where(x =>
                        x.Order.delivery_date >= start &&
                        x.Order.delivery_date <= end
                    );
                }


                if (!string.IsNullOrEmpty(servReq))
                {
                    query = query.Where(x => x.Job.serv_req == servReq);
                }

                if (!string.IsNullOrEmpty(uom))
                {
                    query = query.Where(x => x.Job.charge_uom == uom);
                }

                if (!string.IsNullOrEmpty(vendorId))
                {
                    query = query.Where(x => x.Job.vendorid == vendorId);
                }

                // Execute query first
                var joinedData = await query
                    .OrderByDescending(x => x.Order.delivery_date)
                    .ToListAsync();

                // Get all order IDs
                var orderIds = joinedData.Select(x => x.Order.id_seq).Distinct().ToList();

                // Get all KM orders for these orders
                var kmOrders = await _context.KMOrders
                    .Where(k => orderIds.Contains(k.order_id.Value))
                    .ToListAsync();

                // Format the result
                var result = joinedData.Select(x => new
                {
                    id_seq = x.Order.id_seq,
                    inv_no = x.Order.inv_no,
                    delivery_date = x.Order.delivery_date.HasValue
                        ? x.Order.delivery_date.Value.ToString("dd/MM/yyyy")
                        : "-",
                    sub_custid = x.Order.sub_custid,
                    dest_area = x.Order.dest_area,
                    serv_req = x.Job.serv_req,
                    uom = x.Job.charge_uom,
                    vendorid = x.Job.vendorid,
                    jobid = x.Job.jobid,
                    kmOrder = kmOrders
                        .Where(k => k.order_id == x.Order.id_seq)
                        .Select(k => new { k.id, k.km })
                        .FirstOrDefault()
                })
                .GroupBy(x => x.id_seq)
                .Select(g => g.First())
                .ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetKmOrder(int orderId)
        {
            var kmOrder = await _context.KMOrders
                .FirstOrDefaultAsync(k => k.order_id == orderId);

            if (kmOrder == null)
            {
                return NotFound();
            }

            return Json(kmOrder);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] KMOrder model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Data tidak valid" });
                }

                // Check if KM already exists for this order
                var exists = await _context.KMOrders
                    .AnyAsync(k => k.order_id == model.order_id);

                if (exists)
                {
                    return Json(new { success = false, message = "KM untuk order ini sudah ada" });
                }

                model.entryuser = HttpContext.Session.GetString("username");
                model.entrydate = DateTime.Now;

                _context.KMOrders.Add(model);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Data berhasil disimpan" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] KMOrder model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Data tidak valid" });
                }

                var kmOrder = await _context.KMOrders.FindAsync(model.id);

                if (kmOrder == null)
                {
                    return Json(new { success = false, message = "Data tidak ditemukan" });
                }

                kmOrder.km = model.km;
                kmOrder.updateuser = HttpContext.Session.GetString("username");
                kmOrder.updatedate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Data berhasil diupdate" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var kmOrder = await _context.KMOrders.FindAsync(id);

                if (kmOrder == null)
                {
                    return Json(new { success = false, message = "Data tidak ditemukan" });
                }

                _context.KMOrders.Remove(kmOrder);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Data berhasil dihapus" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}