using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Models;

public class VendorTruckController : Controller
{
    private readonly AppDbContext _context;

    public VendorTruckController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var list = _context.VendorTrucks.ToList();
        return View(list);
    }

    public IActionResult Create()
    {
        return View("Form", new VendorTruck { 
            sup_code = string.Empty,
            vehicle_no  = string.Empty,
        });
    }

    [HttpPost]
    public IActionResult Create(VendorTruck model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        model.entry_date = DateTime.Now;
        model.entry_user = User.Identity?.Name ?? "system";

        _context.VendorTrucks.Add(model);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }

    public IActionResult Edit(int id)
    {
        var data = _context.VendorTrucks.FirstOrDefault(x => x.ID == id);
        if (data == null) return NotFound();

        return View("Form", data);
    }

    [HttpPost]
    public IActionResult Edit(VendorTruck model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var existing = _context.VendorTrucks.FirstOrDefault(x => x.ID == model.ID);
        if (existing == null) return NotFound();

        // Update field
        existing.sup_code = model.sup_code;
        existing.vehicle_no = model.vehicle_no;
        existing.vehicle_merk = model.vehicle_merk;
        existing.vehicle_type = model.vehicle_type;
        existing.vehicle_doortype = model.vehicle_doortype;
        existing.vehicle_size = model.vehicle_size;
        existing.vehicle_driver = model.vehicle_driver;
        existing.vehicle_STNK = model.vehicle_STNK;
        existing.vehicle_STNK_exp = model.vehicle_STNK_exp;
        existing.vehicle_KIR = model.vehicle_KIR;
        existing.vehicle_KIR_exp = model.vehicle_KIR_exp;
        existing.vehicle_emisi = model.vehicle_emisi;
        existing.vehicle_KTP = model.vehicle_KTP;
        existing.vehicle_SIM = model.vehicle_SIM;
        existing.vehicle_remark = model.vehicle_remark;
        existing.vehicle_active = model.vehicle_active;

        existing.update_date = DateTime.Now;
        existing.update_user = User.Identity?.Name ?? "system";

        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.VendorTrucks.FirstOrDefault(x => x.ID == id);
        if (data == null) return NotFound();

        _context.VendorTrucks.Remove(data);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }
}
