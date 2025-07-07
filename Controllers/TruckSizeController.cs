using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;


[SessionAuthorize]
public class TruckSizeController : Controller
{
    private readonly AppDbContext _context;

    public TruckSizeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var list = _context.TruckSizes.ToList();
        return View(list);
    }

    public IActionResult Form(int? id)
    {
        if (id == null)
        {
            return PartialView("_Form", new TruckSize
            {
                trucksize_code = string.Empty
            });
        }

        var data = _context.TruckSizes.FirstOrDefault(t => t.ID == id);
        return PartialView("_Form", data);
    }

    [HttpPost]
    public IActionResult Form(TruckSize model)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var existing = _context.TruckSizes.FirstOrDefault(t => t.ID == model.ID);

        if (existing == null)
        {
            model.entrydate = DateTime.Now;
            _context.TruckSizes.Add(model);
        }
        else
        {
            existing.trucksize_code = model.trucksize_code;
            existing.updateuser = model.updateuser;
            existing.updatedate = DateTime.Now;
        }

        _context.SaveChanges();
        return Ok();
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.TruckSizes.FirstOrDefault(t => t.ID == id);
        if (data == null) return NotFound();

        _context.TruckSizes.Remove(data);
        _context.SaveChanges();

        return Ok();
    }
}
