using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class PriceSellController : Controller
{
    private readonly AppDbContext _context;
    private readonly SelectListService _selectList;

    public PriceSellController(AppDbContext context, SelectListService selectList)
    {
        _context = context;
        _selectList = selectList;
    }

    public IActionResult Index()
    {
        return View(_context.PriceSells.ToList());
    }

    public IActionResult Form(int? id)
    {
        ViewBag.ListCustomer = _selectList.getCustomers();
        ViewBag.ListOrigin = _selectList.GetOrigins();
        ViewBag.ListDestination = _selectList.GetDestinations();
        ViewBag.ListServiceType = _selectList.GetServiceTypes();
        ViewBag.ListServiceModa = _selectList.GetServiceModas();
        ViewBag.ListTruckSize = _selectList.GetTruckSizes();
        ViewBag.ListChargeUom = _selectList.GetChargeUoms();
        var model = id == null ? new PriceSell() : _context.PriceSells.Find(id);
        return PartialView("_Form", model);
    }

    [HttpPost]
    public IActionResult Save(PriceSell model)
    {
        if (!ModelState.IsValid)
            return PartialView("_Form", model);

        if (model.id_seq == 0)
        {
            model.entry_date = DateTime.Now;
            model.entry_user = HttpContext.Session.GetString("username") ?? "System";
            _context.PriceSells.Add(model);
        }
        else
        {
            var db = _context.PriceSells.Find(model.id_seq);
            if (db == null) return NotFound();

            db.cust_code = model.cust_code;
            db.origin = model.origin;
            db.dest = model.dest;
            db.serv_type = model.serv_type;
            db.serv_moda = model.serv_moda;
            db.truck_size = model.truck_size;
            db.charge_uom = model.charge_uom;
            db.sell1 = model.sell1;
            db.valid_date = model.valid_date;
            db.active_flag = model.active_flag;
            db.curr = model.curr;
            db.rate_value = model.rate_value;
            db.update_date = DateTime.Now;
            db.update_user = HttpContext.Session.GetString("username") ?? "System";
        }

        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var model = _context.PriceSells.Find(id);
        if (model == null) return NotFound();

        _context.PriceSells.Remove(model);
        _context.SaveChanges();
        return Ok();
    }

    public IActionResult ExportExcel()
    {
        var data = _context.PriceSells.ToList();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("PriceSell");
        worksheet.Cell(1, 1).InsertTable(data);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"PriceSell_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
