using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class OriginController : Controller
{
    private readonly AppDbContext _context;

    public OriginController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var list = _context.Origins.ToList();
        return View(list);
    }

    public IActionResult Form(int? id)
    {
        if (id == null)
        {
            return PartialView("_Form", new Origin
            {
                origin_code = string.Empty
            });
        }

        var data = _context.Origins.FirstOrDefault(o => o.id == id);
        return PartialView("_Form", data);
    }

    [HttpPost]
    public IActionResult Form(Origin model)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var existing = _context.Origins.FirstOrDefault(o => o.id == model.id);

        if (existing == null)
        {
            model.entryuser = HttpContext.Session.GetString("username") ?? "System";
            model.entrydate = DateTime.Now;
            _context.Origins.Add(model);
        }
        else
        {
            existing.origin_code = model.origin_code;
            existing.origin_loccode = model.origin_loccode;
            existing.updateuser = HttpContext.Session.GetString("username") ?? "System";
            existing.updatedate = DateTime.Now;
        }

        _context.SaveChanges();
        return Ok();
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.Origins.FirstOrDefault(o => o.id == id);
        if (data == null) return NotFound();

        _context.Origins.Remove(data);
        _context.SaveChanges();

        return Ok();
    }
}
