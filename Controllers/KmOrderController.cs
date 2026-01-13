using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Models;

namespace TMSBilling.Controllers
{
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
            ViewBag.ServReqList = await _context.Orders
                .Where(o => !string.IsNullOrEmpty(o.serv_req))
                .Select(o => o.serv_req)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            ViewBag.UomList = await _context.Orders
                .Where(o => !string.IsNullOrEmpty(o.uom))
                .Select(o => o.uom)
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetFilteredData(
            DateTime? startDate,
            DateTime? endDate,
            string? servReq,
            string? uom)
        {
            try
            {
                var query = _context.Orders.AsQueryable();

                // Apply filters
                if (startDate.HasValue)
                {
                    query = query.Where(o => o.delivery_date >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(o => o.delivery_date <= endDate.Value);
                }

                if (!string.IsNullOrEmpty(servReq))
                {
                    query = query.Where(o => o.serv_req == servReq);
                }

                if (!string.IsNullOrEmpty(uom))
                {
                    query = query.Where(o => o.uom == uom);
                }

                // Execute query first, then do the formatting in memory
                var ordersData = await query
                    .OrderByDescending(o => o.delivery_date)
                    .ToListAsync();

                // Get all KM orders for these orders
                var orderIds = ordersData.Select(o => o.id_seq).ToList();
                var kmOrders = await _context.KMOrders
                    .Where(k => orderIds.Contains(k.order_id.Value))
                    .ToListAsync();

                // Format the result
                var result = ordersData.Select(o => new
                {
                    o.id_seq,
                    o.inv_no,
                    delivery_date = o.delivery_date.HasValue
                        ? o.delivery_date.Value.ToString("dd/MM/yyyy")
                        : "-",
                    o.sub_custid,
                    o.dest_area,
                    o.serv_req,
                    o.uom,
                    kmOrder = kmOrders
                        .Where(k => k.order_id == o.id_seq)
                        .Select(k => new { k.id, k.km })
                        .FirstOrDefault()
                }).ToList();

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