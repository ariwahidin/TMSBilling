using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class VendorTruckEmailController : Controller
{
    private readonly AppDbContext _context;

    public VendorTruckEmailController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var list = _context.VendorTruckEmails
            .OrderBy(x => x.sup_code)
            .ThenBy(x => x.vehicle_no)
            .ThenBy(x => x.email_type)
            .ToList();

        return View(list);
    }

    public IActionResult Form(int? id)
    {
        SetDropdownLists();

        if (id == null)
        {
            return View("Form", new VendorTruckEmail
            {
                sup_code = string.Empty,
                vehicle_no = string.Empty,
                email_address = string.Empty,
                email_type = string.Empty,
                is_active = 1
            });
        }

        var data = _context.VendorTruckEmails.Find(id);
        if (data == null) return NotFound();

        return View("Form", data);
    }

    [HttpPost]
    public IActionResult Create(VendorTruckEmail model)
    {
        SetDropdownLists();

        if (!ModelState.IsValid)
            return View("Form", model);

        var isDuplicate = _context.VendorTruckEmails.Any(x =>
            x.vehicle_no == model.vehicle_no &&
            x.email_address == model.email_address &&
            x.email_type == model.email_type);

        if (isDuplicate)
        {
            ModelState.AddModelError("email_address", "Email with the same type already exists for this truck.");
            return View("Form", model);
        }

        model.entry_date = DateTime.Now;
        model.entry_user = HttpContext.Session.GetString("username") ?? "System";

        _context.VendorTruckEmails.Add(model);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }

    public IActionResult Edit(int id)
    {
        var data = _context.VendorTruckEmails.FirstOrDefault(x => x.ID == id);
        if (data == null) return NotFound();

        SetDropdownLists(data.sup_code);
        return View("Form", data);
    }

    [HttpPost]
    public IActionResult Edit(VendorTruckEmail model)
    {
        SetDropdownLists(model.sup_code);

        if (!ModelState.IsValid)
            return View("Form", model);

        var existing = _context.VendorTruckEmails.FirstOrDefault(x => x.ID == model.ID);
        if (existing == null) return NotFound();

        var isDuplicate = _context.VendorTruckEmails.Any(x =>
            x.vehicle_no == model.vehicle_no &&
            x.email_address == model.email_address &&
            x.email_type == model.email_type &&
            x.ID != model.ID);

        if (isDuplicate)
        {
            ModelState.AddModelError("email_address", "Email with the same type already exists for this truck.");
            return View("Form", model);
        }

        existing.sup_code = model.sup_code;
        existing.vehicle_no = model.vehicle_no;
        existing.email_address = model.email_address;
        existing.email_type = model.email_type;
        existing.email_name = model.email_name;
        existing.is_active = model.is_active;
        existing.remark = model.remark;

        existing.update_date = DateTime.Now;
        existing.update_user = HttpContext.Session.GetString("username") ?? "System";

        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.VendorTruckEmails.FirstOrDefault(x => x.ID == id);
        if (data == null) return NotFound();

        _context.VendorTruckEmails.Remove(data);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    // AJAX endpoint: get trucks filtered by vendor
    [HttpGet]
    public JsonResult GetTrucksByVendor(string supCode)
    {
        var trucks = _context.VendorTrucks
            .Where(x => x.sup_code == supCode && x.vehicle_active == 1)
            .Select(x => new { value = x.vehicle_no, text = x.vehicle_no })
            .ToList();

        return Json(trucks);
    }

    private void SetDropdownLists(string? selectedSupCode = null)
    {
        ViewBag.ListVendor = _context.Vendors
            .Select(v => new SelectListItem
            {
                Value = v.SUP_CODE,
                Text = $"{v.SUP_CODE} - {v.SUP_NAME}"
            }).ToList();

        // Load trucks for currently selected vendor (for edit mode)
        if (!string.IsNullOrEmpty(selectedSupCode))
        {
            ViewBag.ListTruck = _context.VendorTrucks
                .Where(x => x.sup_code == selectedSupCode)
                .Select(x => new SelectListItem
                {
                    Value = x.vehicle_no,
                    Text = x.vehicle_no
                }).ToList();
        }
        else
        {
            ViewBag.ListTruck = new List<SelectListItem>();
        }

        ViewBag.ListEmailType = new List<SelectListItem>
        {
            new SelectListItem { Value = "TO", Text = "TO" },
            new SelectListItem { Value = "CC", Text = "CC" },
        };
    }
}