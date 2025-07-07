using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using System.Text;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class VendorController : Controller
{
    private readonly AppDbContext _context;

    public VendorController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var vendors = _context.Vendors.ToList();
        return View(vendors);
    }

    public IActionResult Create()
    {
        return View("Form", new Vendor
        {
            SUP_CODE = string.Empty // atau nilai default sesuai kebutuhan
        });
    }

    [HttpPost]
    public IActionResult Create(Vendor model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        model.ENTRY_USER = HttpContext.Session.GetString("username") ?? "System";
        model.ENTRY_DATE = DateTime.Now;
        _context.Vendors.Add(model);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    public IActionResult Edit(int id)
    {
        var data = _context.Vendors.FirstOrDefault(v => v.ID == id);
        if (data == null) return NotFound();
        return View("Form", data);
    }

    [HttpPost]
    public IActionResult Edit(Vendor model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var existing = _context.Vendors.FirstOrDefault(v => v.ID == model.ID);
        if (existing == null) return NotFound();

        existing.SUP_CODE = model.SUP_CODE;
        existing.SUP_TYPE = model.SUP_TYPE;
        existing.SUP_NAME = model.SUP_NAME;
        existing.SUP_ADDR1 = model.SUP_ADDR1;
        existing.SUP_ADDR2 = model.SUP_ADDR2;
        existing.SUP_CITY = model.SUP_CITY;
        existing.SUP_EMAIL = model.SUP_EMAIL;
        existing.SUP_TEL = model.SUP_TEL;
        existing.SUP_FAX = model.SUP_FAX;
        existing.SUP_PIC = model.SUP_PIC;
        existing.TAX_REG_NO = model.TAX_REG_NO;
        existing.ACTIVE_FLAG = model.ACTIVE_FLAG;
        existing.UPDATE_USER = HttpContext.Session.GetString("username") ?? "System";
        existing.UPDATE_DATE = DateTime.Now;

        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var vendor = _context.Vendors.FirstOrDefault(v => v.ID == id);
        if (vendor == null) return NotFound();

        _context.Vendors.Remove(vendor);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }



}
