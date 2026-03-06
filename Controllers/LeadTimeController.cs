using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class LeadTimeController : Controller
{
    private readonly AppDbContext _context;

    public LeadTimeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var list = _context.LeadTimes.ToList();
        return View(list);
    }

    public IActionResult Form(int? id)
    {
        PopulateDropdowns();

        if (id == null)
        {
            return PartialView("_Form", new LeadTime());
        }

        var data = _context.LeadTimes.FirstOrDefault(o => o.id_seq == id);
        if (data == null) return NotFound();

        return PartialView("_Form", data);
    }

    [HttpPost]
    public IActionResult Form(LeadTime model)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var existing = _context.LeadTimes.FirstOrDefault(o => o.id_seq == model.id_seq);

        if (existing == null)
        {
            bool duplicate = IsDuplicate(model.cust_code, model.origin, model.dest, model.serv_type, model.serv_moda, model.truck_size);
            if (duplicate)
                return BadRequest(new { message = "Lead time for this combination already exists." });

            model.entry_user = HttpContext.Session.GetString("username") ?? "System";
            model.entry_date = DateTime.Now;
            model.active_flag = 1;
            _context.LeadTimes.Add(model);
        }
        else
        {
            bool duplicate = IsDuplicate(model.cust_code, model.origin, model.dest, model.serv_type, model.serv_moda, model.truck_size, model.id_seq);
            if (duplicate)
                return BadRequest(new { message = "Lead time for this combination already exists on another record." });

            existing.cust_code = model.cust_code;
            existing.origin = model.origin;
            existing.dest = model.dest;
            existing.serv_type = model.serv_type;
            existing.serv_moda = model.serv_moda;
            existing.truck_size = model.truck_size;
            existing.delivery_days = model.delivery_days;
            existing.pod_days = model.pod_days;
            existing.active_flag = model.active_flag;
            existing.update_user = HttpContext.Session.GetString("username") ?? "System";
            existing.update_date = DateTime.Now;
        }

        _context.SaveChanges();
        return Ok();
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.LeadTimes.FirstOrDefault(o => o.id_seq == id);
        if (data == null) return NotFound();

        _context.LeadTimes.Remove(data);
        _context.SaveChanges();
        return Ok();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private bool IsDuplicate(string? custCode, string? origin, string? dest,
        string? servType, string? servModa, string? truckSize, int? excludeId = null)
    {
        var query = _context.LeadTimes.Where(x =>
            x.cust_code == custCode &&
            x.origin == origin &&
            x.dest == dest &&
            x.serv_type == servType &&
            x.serv_moda == servModa &&
            x.truck_size == truckSize);

        if (excludeId.HasValue)
            query = query.Where(x => x.id_seq != excludeId.Value);

        return query.Any();
    }

    private void PopulateDropdowns()
    {
        ViewBag.Origins = _context.Origins.OrderBy(o => o.origin_code).ToList();
        ViewBag.Destinations = _context.Destinations.OrderBy(d => d.destination_code).ToList();
        ViewBag.Customers = _context.CustomerMains
                                       .Where(c => c.STATUS_FLAG == 1)
                                       .OrderBy(c => c.CUST_NAME)
                                       .ToList();
        ViewBag.ServiceTypes = _context.ServiceTypes.OrderBy(s => s.serv_name).ToList();
        ViewBag.ServiceModas = _context.ServiceModas.OrderBy(s => s.moda_name).ToList();
        ViewBag.TruckSizes = _context.TruckSizes.OrderBy(t => t.trucksize_code).ToList();
    }
}