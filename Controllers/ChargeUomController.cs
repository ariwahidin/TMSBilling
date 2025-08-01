using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class ChargeUomController : Controller
    {
        private readonly AppDbContext _context;

        public ChargeUomController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var data = _context.ChargeUoms.ToList();
            return View(data);
        }

        public IActionResult Form(int? id)
        {
            var model = id == null || id == 0
                ? new ChargeUom { charge_name = string.Empty }
                : _context.ChargeUoms.FirstOrDefault(x => x.id_seq == id) ?? new ChargeUom { charge_name = string.Empty };

            return PartialView("_Form", model);
        }

        [HttpPost]
        public IActionResult Save(ChargeUom model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = "Invalid input data"
                });

            if (model.id_seq == 0)
            {

                bool exists = _context.ChargeUoms.Any(v => v.charge_name == model.charge_name);
                if (exists)
                {
                    return BadRequest(new
                    {
                        message = "Charge UoM already exists"
                    });
                }

                model.entry_date = DateTime.Now;
                model.entry_user = HttpContext.Session.GetString("username") ?? "system";
                _context.ChargeUoms.Add(model);
            }
            else
            {
                bool duplicate = _context.ChargeUoms.Any(v => v.charge_name == model.charge_name && v.id_seq != model.id_seq);
                if (duplicate)
                {

                    return BadRequest(new
                    {
                        message = "Charge uom already exists on another record"
                    });
                }

                var existing = _context.ChargeUoms.FirstOrDefault(x => x.id_seq == model.id_seq);
                if (existing == null) return NotFound();

                existing.charge_name = model.charge_name;
                existing.entry_date = DateTime.Now;
                existing.entry_user = HttpContext.Session.GetString("username") ?? "system";
            }

            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = _context.ChargeUoms.Find(id);
            if (item == null) return NotFound();

            _context.ChargeUoms.Remove(item);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
