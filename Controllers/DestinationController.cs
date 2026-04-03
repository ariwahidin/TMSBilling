using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class DestinationController : Controller
{
    private readonly AppDbContext _context;
    private readonly SelectListService _selectList;

    public DestinationController(AppDbContext context, SelectListService selectList)
    {
        _context = context;
        _selectList = selectList;
    }

    public IActionResult Index()
    {
        var username = HttpContext.Session.GetString("username") ?? "System";
        var accessibleCustomers = _context.UserXCustomers
        .Where(x => x.UserName == username)
        .Select(x => x.CustomerMain)
        .Distinct()
        .ToList();

        var list = _context.Destinations
            .Where(d => accessibleCustomers.Contains(d.MAIN_CUST))
            .ToList();
        return View(list);
    }

    public IActionResult Form(int? id)
    {
        ViewBag.ListArea = _selectList.getArea();
        ViewBag.ListCustomer = _context.CustomerMains
            .OrderBy(c => c.MAIN_CUST)
            .Select(c => new SelectListItem
            {
                Value = c.MAIN_CUST,
                Text = c.MAIN_CUST
            }).ToList();

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
            bool exists = _context.Destinations.Any(v =>
                v.destination_code == model.destination_code &&
                v.MAIN_CUST == model.MAIN_CUST);
            if (exists)
                return BadRequest(new { message = "Destination sudah ada untuk customer ini" });

            model.entryuser = HttpContext.Session.GetString("username") ?? "System";
            model.entrydate = DateTime.Now;
            _context.Destinations.Add(model);
        }
        else
        {
            bool duplicate = _context.Destinations.Any(v =>
                v.destination_code == model.destination_code &&
                v.MAIN_CUST == model.MAIN_CUST &&
                v.ID != model.ID);
            if (duplicate)
                return BadRequest(new { message = "Destination sudah ada pada record lain" });

            existing.destination_code = model.destination_code;
            existing.dest_loccode = model.dest_loccode;
            existing.area = model.area;
            existing.MAIN_CUST = model.MAIN_CUST;
            existing.updateuser = HttpContext.Session.GetString("username") ?? "System";
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