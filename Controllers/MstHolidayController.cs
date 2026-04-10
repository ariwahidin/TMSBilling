using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class MstHolidayController : Controller
{
    private readonly AppDbContext _context;

    public MstHolidayController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var list = _context.MstHolidays.OrderBy(h => h.HolDate).ToList();
        return View(list);
    }

    public IActionResult Form(int? id)
    {
        if (id == null)
            return PartialView("_Form", new MstHoliday { IsActive = true });

        var data = _context.MstHolidays.FirstOrDefault(h => h.IdSeq == id);
        if (data == null) return NotFound();
        return PartialView("_Form", data);
    }

    [HttpPost]
    public IActionResult Form(MstHoliday model)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Data tidak valid." });

        var existing = _context.MstHolidays.FirstOrDefault(h => h.IdSeq == model.IdSeq);
        if (existing == null)
        {
            bool duplicate = _context.MstHolidays.Any(h => h.HolDate == model.HolDate);
            if (duplicate)
                return BadRequest(new { message = "Tanggal holiday sudah terdaftar." });

            _context.MstHolidays.Add(model);
        }
        else
        {
            bool duplicate = _context.MstHolidays.Any(h => h.HolDate == model.HolDate && h.IdSeq != model.IdSeq);
            if (duplicate)
                return BadRequest(new { message = "Tanggal holiday sudah terdaftar pada record lain." });

            existing.HolDate = model.HolDate;
            existing.HolName = model.HolName;
            existing.Description = model.Description;
            existing.IsActive = model.IsActive;
        }

        _context.SaveChanges();
        return Ok();
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.MstHolidays.FirstOrDefault(h => h.IdSeq == id);
        if (data == null) return NotFound();
        _context.MstHolidays.Remove(data);
        _context.SaveChanges();
        return Ok();
    }

    // ── Bulk Actions ──────────────────────────────────────────────
    [HttpPost]
    public IActionResult BulkSetActive([FromBody] BulkActiveRequest req)
    {
        if (req.Ids == null || !req.Ids.Any())
            return BadRequest(new { message = "Tidak ada data dipilih." });

        var rows = _context.MstHolidays.Where(h => req.Ids.Contains(h.IdSeq)).ToList();
        rows.ForEach(h => h.IsActive = req.IsActive);
        _context.SaveChanges();
        return Ok(new { updated = rows.Count });
    }

    [HttpPost]
    public IActionResult BulkDelete([FromBody] List<int> ids)
    {
        if (ids == null || !ids.Any())
            return BadRequest(new { message = "Tidak ada data dipilih." });

        var rows = _context.MstHolidays.Where(h => ids.Contains(h.IdSeq)).ToList();
        _context.MstHolidays.RemoveRange(rows);
        _context.SaveChanges();
        return Ok(new { deleted = rows.Count });
    }
}

public class BulkActiveRequest
{
    public List<int> Ids { get; set; } = new();
    public bool IsActive { get; set; }
}