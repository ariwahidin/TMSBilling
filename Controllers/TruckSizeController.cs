using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;


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

        //if (!ModelState.IsValid)
        //{
        //    var errors = ModelState.Values
        //        .SelectMany(v => v.Errors)
        //        .Select(e => e.ErrorMessage)
        //        .ToList();

        //    return BadRequest(new
        //    {
        //        message = "Please correct the following errors:",
        //        errors
        //    });
        //}


        var existing = _context.TruckSizes.FirstOrDefault(t => t.ID == model.ID);

        if (existing == null)
        {
            bool exists = _context.TruckSizes.Any(v => v.trucksize_code == model.trucksize_code);
            if (exists)
            {
                return BadRequest(new
                {
                    message = "Truck Size ID already exists"
                });
            }

            model.entryuser= HttpContext.Session.GetString("username") ?? "System";
            model.entrydate = DateTime.Now;
            _context.TruckSizes.Add(model);
        }
        else
        {

            // Cek apakah SUP_CODE digunakan oleh vendor lain
            bool duplicate = _context.TruckSizes.Any(v => v.trucksize_code == model.trucksize_code && v.ID != model.ID);
            if (duplicate)
            {
                return BadRequest(new
                {
                    message = "Truck Size ID already exists on another record"
                });
            }


            existing.trucksize_code = model.trucksize_code;
            existing.updateuser = HttpContext.Session.GetString("username") ?? "System";
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
