using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Models;

public class WarehouseController : Controller
{
    private readonly AppDbContext _context;

    public WarehouseController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var list = _context.Warehouses.ToList();
        return View(list);
    }

    public IActionResult Form(int? id)
    {
        if (id == null)
        {
            return PartialView("_Form", new Warehouse
            {
                wh_code = string.Empty
            });
        }

        var data = _context.Warehouses.FirstOrDefault(w => w.ID == id);
        return PartialView("_Form", data);
    }

    [HttpPost]
    public IActionResult Form(Warehouse model)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var existing = _context.Warehouses.FirstOrDefault(w => w.ID == model.ID);

        if (existing == null)
        {
            model.entrydate = DateTime.Now;
            _context.Warehouses.Add(model);
        }
        else
        {
            existing.wh_code = model.wh_code;
            existing.wh_name = model.wh_name;
            existing.updateuser = model.updateuser;
            existing.updatedate = DateTime.Now;
        }

        _context.SaveChanges();
        return Ok();
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.Warehouses.FirstOrDefault(w => w.ID == id);
        if (data == null) return NotFound();

        _context.Warehouses.Remove(data);
        _context.SaveChanges();

        return Ok();
    }
}
