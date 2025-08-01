//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using TMSBilling.Data;
//using TMSBilling.Filters;
//using TMSBilling.Models;


//[SessionAuthorize]
//public class VendorTruckController : Controller
//{
//    private readonly AppDbContext _context;

//    public VendorTruckController(AppDbContext context)
//    {
//        _context = context;
//    }

//    public IActionResult Index()
//    {
//        var list = _context.VendorTrucks.ToList();
//        return View(list);
//    }

//    public IActionResult Form(int? id)
//    {
//        var listVendor = _context.Vendors
//            .Select(v => new SelectListItem {
//                Value = v.SUP_CODE,
//                Text = $"{v.SUP_CODE} - {v.SUP_NAME}"
//            }).ToList();

//        var listCapacity = _context.TruckSizes
//            .Select(x => new SelectListItem {
//                Value = x.trucksize_code,
//                Text = x.trucksize_code
//            }).ToList();

//        var listMerk = new List<SelectListItem> { 
//            new SelectListItem {Value = "DAIHATSU", Text = "DAIHATSU"},
//            new SelectListItem {Value = "GRANDMAX", Text = "GRANDMAX"},
//            new SelectListItem {Value = "HINO", Text = "HINO"},
//            new SelectListItem {Value = "ISUZU", Text = "ISUZU"},
//            new SelectListItem {Value = "MITSUBISHI", Text = "MITSUBISHI"},
//            new SelectListItem {Value = "NISSAN", Text = "NISSAN"},
//            new SelectListItem {Value = "SUZUKI", Text = "SUZUKI"},
//            new SelectListItem {Value = "TOYOTA", Text = "TOYOTA"},
//            new SelectListItem {Value = "UD QUESTER", Text = "UD QUESTER"},
//        };

//        var listType = new List<SelectListItem>
//        {
//            new SelectListItem {Value = "BLIND VAN", Text = "BLIND VAN"},
//            new SelectListItem {Value = "BUILT UP", Text = "BUILT UP"},
//            new SelectListItem {Value = "CARRY", Text = "CARRY"},
//            new SelectListItem {Value = "CDD", Text = "CDD"},
//            new SelectListItem {Value = "CDD LONG", Text = "CDD LONG"},
//            new SelectListItem {Value = "CDE", Text = "CDE"},
//            new SelectListItem {Value = "FUSO", Text = "FUSO"},
//            new SelectListItem {Value = "HINO", Text = "HINO"},
//            new SelectListItem {Value = "L300", Text = "L300"},
//            new SelectListItem {Value = "TRONTON", Text = "TRONTON"},
//            new SelectListItem {Value = "WINGBOX", Text = "WINGBOX"},
//        };

//        var listDoorType = new List<SelectListItem> {
//            new SelectListItem {Value = "PINTU BELAKANG", Text =  "PINTU BELAKANG"},
//            new SelectListItem {Value = "PINTU DEPAN", Text = "PINTU DEPAN" },
//            new SelectListItem {Value = "WINGBOX", Text = "WINGBOX"},
//        };

//        ViewBag.ListVendor = listVendor;
//        ViewBag.ListMerk = listMerk;
//        ViewBag.ListType = listType;
//        ViewBag.ListDoorType = listDoorType;
//        ViewBag.ListCapacity = listCapacity;

//        if(id == null)
//            return View("Form", new VendorTruck
//            {
//                sup_code = string.Empty,
//                vehicle_no = string.Empty,
//            });

//        var data = _context.VendorTrucks.Find(id);

//        if (data == null) 
//            return NotFound();

//        return View("Form", data);
//    }

//    [HttpPost]
//    public IActionResult Create(VendorTruck model)
//    {
//        if (!ModelState.IsValid)
//            return View("Form", model);

//        // Check if vehicle_no already exists
//        var isDuplicate = _context.VendorTrucks.Any(x => x.vehicle_no == model.vehicle_no);
//        if (isDuplicate)
//        {
//            ModelState.AddModelError("vehicle_no", "Vehicle number already exists.");
//            return View("Form", model);
//        }

//        model.entry_date = DateTime.Now;
//        model.entry_user = HttpContext.Session.GetString("username") ?? "System";

//        _context.VendorTrucks.Add(model);
//        _context.SaveChanges();

//        return RedirectToAction("Index");
//    }


//    public IActionResult Edit(int id)
//    {
//        var data = _context.VendorTrucks.FirstOrDefault(x => x.ID == id);
//        if (data == null) return NotFound();

//        return View("Form", data);
//    }

//    [HttpPost]
//    public IActionResult Edit(VendorTruck model)
//    {
//        if (!ModelState.IsValid) return View("Form", model);

//        var existing = _context.VendorTrucks.FirstOrDefault(x => x.ID == model.ID);
//        if (existing == null) return NotFound();

//        // Check if another record has the same vehicle_no
//        var isDuplicate = _context.VendorTrucks.Any(x => x.vehicle_no == model.vehicle_no && x.ID != model.ID);
//        if (isDuplicate)
//        {
//            ModelState.AddModelError("vehicle_no", "Vehicle number already exists.");
//            return View("Form", model);
//        }

//        // Update field
//        existing.sup_code = model.sup_code;
//        existing.vehicle_no = model.vehicle_no;
//        existing.vehicle_merk = model.vehicle_merk;
//        existing.vehicle_type = model.vehicle_type;
//        existing.vehicle_doortype = model.vehicle_doortype;
//        existing.vehicle_size = model.vehicle_size;
//        existing.vehicle_driver = model.vehicle_driver;
//        existing.vehicle_STNK = model.vehicle_STNK;
//        existing.vehicle_STNK_exp = model.vehicle_STNK_exp;
//        existing.vehicle_KIR = model.vehicle_KIR;
//        existing.vehicle_KIR_exp = model.vehicle_KIR_exp;
//        existing.vehicle_emisi = model.vehicle_emisi;
//        existing.vehicle_KTP = model.vehicle_KTP;
//        existing.vehicle_SIM = model.vehicle_SIM;
//        existing.vehicle_remark = model.vehicle_remark;
//        existing.vehicle_active = model.vehicle_active;

//        existing.update_date = DateTime.Now;
//        existing.update_user = HttpContext.Session.GetString("username") ?? "System";

//        _context.SaveChanges();
//        return RedirectToAction("Index");
//    }

//    [HttpPost]
//    public IActionResult Delete(int id)
//    {
//        var data = _context.VendorTrucks.FirstOrDefault(x => x.ID == id);
//        if (data == null) return NotFound();

//        _context.VendorTrucks.Remove(data);
//        _context.SaveChanges();
//        return RedirectToAction("Index");
//    }
//}



using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using System.Linq;

[SessionAuthorize]
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

    public IActionResult Form(int? id)
    {
        SetDropdownLists();

        if (id == null)
        {
            return View("Form", new VendorTruck
            {
                sup_code = string.Empty,
                vehicle_no = string.Empty,
            });
        }

        var data = _context.VendorTrucks.Find(id);

        if (data == null)
            return NotFound();

        return View("Form", data);
    }

    [HttpPost]
    public IActionResult Create(VendorTruck model)
    {
        SetDropdownLists();

        if (!ModelState.IsValid)
            return View("Form", model);

        var isDuplicate = _context.VendorTrucks.Any(x => x.vehicle_no == model.vehicle_no);
        if (isDuplicate)
        {
            ModelState.AddModelError("vehicle_no", "Truck ID already exists.");
            return View("Form", model);
        }

        model.entry_date = DateTime.Now;
        model.entry_user = HttpContext.Session.GetString("username") ?? "System";

        _context.VendorTrucks.Add(model);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }

    public IActionResult Edit(int id)
    {
        var data = _context.VendorTrucks.FirstOrDefault(x => x.ID == id);
        if (data == null) return NotFound();

        SetDropdownLists();
        return View("Form", data);
    }

    [HttpPost]
    public IActionResult Edit(VendorTruck model)
    {
        SetDropdownLists();

        if (!ModelState.IsValid)
            return View("Form", model);

        var existing = _context.VendorTrucks.FirstOrDefault(x => x.ID == model.ID);
        if (existing == null) return NotFound();

        var isDuplicate = _context.VendorTrucks.Any(x => x.vehicle_no == model.vehicle_no && x.ID != model.ID);
        if (isDuplicate)
        {
            ModelState.AddModelError("vehicle_no", "Truck ID already exists.");
            return View("Form", model);
        }

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
        existing.update_user = HttpContext.Session.GetString("username") ?? "System";

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

    private void SetDropdownLists()
    {
        ViewBag.ListVendor = _context.Vendors
            .Select(v => new SelectListItem
            {
                Value = v.SUP_CODE,
                Text = $"{v.SUP_CODE} - {v.SUP_NAME}"
            }).ToList();

        ViewBag.ListCapacity = _context.TruckSizes
            .Select(x => new SelectListItem
            {
                Value = x.trucksize_code,
                Text = x.trucksize_code
            }).ToList();

        ViewBag.ListMerk = new List<SelectListItem> {
            new SelectListItem {Value = "DAIHATSU", Text = "DAIHATSU"},
            new SelectListItem {Value = "GRANDMAX", Text = "GRANDMAX"},
            new SelectListItem {Value = "HINO", Text = "HINO"},
            new SelectListItem {Value = "ISUZU", Text = "ISUZU"},
            new SelectListItem {Value = "MITSUBISHI", Text = "MITSUBISHI"},
            new SelectListItem {Value = "NISSAN", Text = "NISSAN"},
            new SelectListItem {Value = "SUZUKI", Text = "SUZUKI"},
            new SelectListItem {Value = "TOYOTA", Text = "TOYOTA"},
            new SelectListItem {Value = "UD QUESTER", Text = "UD QUESTER"},
        };

        ViewBag.ListType = new List<SelectListItem> {
            new SelectListItem {Value = "BLIND VAN", Text = "BLIND VAN"},
            new SelectListItem {Value = "BUILT UP", Text = "BUILT UP"},
            new SelectListItem {Value = "CARRY", Text = "CARRY"},
            new SelectListItem {Value = "CDD", Text = "CDD"},
            new SelectListItem {Value = "CDD LONG", Text = "CDD LONG"},
            new SelectListItem {Value = "CDE", Text = "CDE"},
            new SelectListItem {Value = "FUSO", Text = "FUSO"},
            new SelectListItem {Value = "HINO", Text = "HINO"},
            new SelectListItem {Value = "L300", Text = "L300"},
            new SelectListItem {Value = "TRONTON", Text = "TRONTON"},
            new SelectListItem {Value = "WINGBOX", Text = "WINGBOX"},
        };

        ViewBag.ListDoorType = new List<SelectListItem> {
            new SelectListItem {Value = "PINTU BELAKANG", Text = "PINTU BELAKANG"},
            new SelectListItem {Value = "PINTU DEPAN", Text = "PINTU DEPAN"},
            new SelectListItem {Value = "WINGBOX", Text = "WINGBOX"},
        };
    }
}

