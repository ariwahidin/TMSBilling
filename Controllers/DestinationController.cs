using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Models;

public class DestinationController : Controller
{
    private readonly AppDbContext _context;

    public DestinationController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var list = _context.Destinations.ToList();
        return View(list);
    }

    public IActionResult Form(int? id)
    {
        if (id == null)
        {
            return PartialView("_Form", new Destination
            {
                destination_code = string.Empty
            });
        }

        var data = _context.Destinations.FirstOrDefault(d => d.ID == id);
        return PartialView("_Form", data);
    }

    [HttpPost]
    public IActionResult Form(Destination model)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var existing = _context.Destinations.FirstOrDefault(d => d.ID == model.ID);

        if (existing == null)
        {
            model.entrydate = DateTime.Now;
            _context.Destinations.Add(model);
        }
        else
        {
            existing.destination_code = model.destination_code;
            existing.dest_loccode = model.dest_loccode;
            existing.updateuser = model.updateuser;
            existing.updatedate = DateTime.Now;
        }

        _context.SaveChanges();
        return Ok();
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.Destinations.FirstOrDefault(d => d.ID == id);
        if (data == null) return NotFound();

        _context.Destinations.Remove(data);
        _context.SaveChanges();

        return Ok();
    }
}
