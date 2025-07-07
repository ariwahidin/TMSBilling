using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class PriceBuyController : Controller
    {
        private readonly AppDbContext _context;

        public PriceBuyController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var data = _context.PriceBuys.OrderByDescending(x => x.entry_date).ToList();
            return View(data);
        }

        public IActionResult Form(int? id)
        {
            var model = id == null || id == 0
                ? new PriceBuy()
                : _context.PriceBuys.FirstOrDefault(x => x.id_seq == id) ?? new PriceBuy();

            return PartialView("_Form", model);
        }

        [HttpPost]
        public IActionResult Save(PriceBuy model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Index");

            if (model.id_seq == 0)
            {
                model.entry_date = DateTime.Now;
                model.entry_user = User.Identity?.Name ?? "system";
                _context.PriceBuys.Add(model);
            }
            else
            {
                var existing = _context.PriceBuys.FirstOrDefault(x => x.id_seq == model.id_seq);
                if (existing == null) return NotFound();

                // Map properti
                existing.sup_code = model.sup_code;
                existing.origin = model.origin;
                existing.dest = model.dest;
                existing.serv_type = model.serv_type;
                existing.serv_moda = model.serv_moda;
                existing.truck_size = model.truck_size;
                existing.charge_uom = model.charge_uom;
                existing.flag_min = model.flag_min;
                existing.charge_min = model.charge_min;
                existing.flag_range = model.flag_range;
                existing.min_range = model.min_range;
                existing.max_range = model.max_range;
                existing.buy1 = model.buy1;
                existing.buy2 = model.buy2;
                existing.buy3 = model.buy3;
                existing.buy_ret_empt = model.buy_ret_empt;
                existing.buy_ret_cargo = model.buy_ret_cargo;
                existing.buy_ovnight = model.buy_ovnight;
                existing.buy_cancel = model.buy_cancel;
                existing.buytrip2 = model.buytrip2;
                existing.buytrip3 = model.buytrip3;
                existing.buy_diff_area = model.buy_diff_area;
                existing.valid_date = model.valid_date;
                existing.curr = model.curr;
                existing.rate_value = model.rate_value;
                existing.active_flag = model.active_flag;
                existing.update_date = DateTime.Now;
                existing.update_user = User.Identity?.Name ?? "system";
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = _context.PriceBuys.Find(id);
            if (item == null) return NotFound();

            _context.PriceBuys.Remove(item);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
