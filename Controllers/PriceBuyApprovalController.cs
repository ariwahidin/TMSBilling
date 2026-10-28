using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Services;

namespace TMSBilling.Controllers;

[SessionAuthorize]
public class PriceBuyApprovalController : Controller
{
    private readonly AppDbContext _db;
    private readonly PriceBuyApprovalService _service;

    public PriceBuyApprovalController(AppDbContext db, PriceBuyApprovalService service)
    {
        _db = db;
        _service = service;
    }

    [HttpGet]
    [RequirePermission("pricebuy_view_approval")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var data = await _db.PriceBuyApprovals
            .AsNoTracking()
            .OrderByDescending(x => x.Status == "PENDING")
            .ThenByDescending(x => x.RequestedAt)
            .ToListAsync(ct);
        return View(data);
    }

    [HttpGet]
    [RequirePermission("pricebuy_view_approval")]
    public async Task<IActionResult> Details(long id, CancellationToken ct)
    {
        var item = await _db.PriceBuyApprovals
            .AsNoTracking()
            .Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.ApprovalId == id, ct);
        if (item == null) return NotFound();
        ViewBag.Audit = await _db.PriceBuyAudits.AsNoTracking()
            .Where(x => x.ApprovalId == id)
            .OrderBy(x => x.PerformedAt).ToListAsync(ct);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("pricebuy_approve")]
    public async Task<IActionResult> Approve(long id, [FromBody] DecisionRequest request, CancellationToken ct)
    {
        try
        {
            await _service.ApproveAsync(id, HttpContext.Session.GetString("username") ?? "", request?.Note, ct);
            return Ok(new { success = true, message = "The approval request was approved and the tariff changes have been applied." });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or DbUpdateException)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("pricebuy_reject")]
    public async Task<IActionResult> Reject(long id, [FromBody] DecisionRequest request, CancellationToken ct)
    {
        try
        {
            await _service.RejectAsync(id, HttpContext.Session.GetString("username") ?? "", request?.Note ?? "", ct);
            return Ok(new { success = true, message = "The approval request was rejected. The official tariff data remains unchanged." });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or DbUpdateException)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("pricebuy_cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        try
        {
            await _service.CancelAsync(id, HttpContext.Session.GetString("username") ?? "", ct);
            return Ok(new { success = true, message = "The approval request was cancelled successfully." });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or UnauthorizedAccessException or DbUpdateException)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    public sealed class DecisionRequest
    {
        public string? Note { get; set; }
    }
}
