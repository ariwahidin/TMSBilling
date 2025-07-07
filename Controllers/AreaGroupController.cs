using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class AreaGroupController : Controller
    {
        private readonly AppDbContext _context;

        public AreaGroupController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var data = _context.AreaGroups.ToList();
            return View(data);
        }

        public IActionResult Form(int? id)
        {
            var model = id == null || id == 0
                ? new AreaGroup { area_name = string.Empty }
                : _context.AreaGroups.FirstOrDefault(x => x.id_seq == id) ?? new AreaGroup { area_name = string.Empty };

            return PartialView("_Form", model);
        }

        [HttpPost]
        public IActionResult Save(AreaGroup model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Index");

            if (model.id_seq == 0)
            {
                model.entry_date = DateTime.Now;
                model.entry_user = HttpContext.Session.GetString("username") ?? "system";
                _context.AreaGroups.Add(model);
            }
            else
            {
                var existing = _context.AreaGroups.FirstOrDefault(x => x.id_seq == model.id_seq);
                if (existing == null) return NotFound();

                existing.area_name = model.area_name;
                existing.entry_date = DateTime.Now;
                existing.entry_user = HttpContext.Session.GetString("username") ?? "system";
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = _context.AreaGroups.Find(id);
            if (item == null) return NotFound();

            _context.AreaGroups.Remove(item);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
