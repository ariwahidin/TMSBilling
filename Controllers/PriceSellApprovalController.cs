using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Services;

namespace TMSBilling.Controllers;

[SessionAuthorize]
public class PriceSellApprovalController : Controller
{
    private readonly AppDbContext _db;
    private readonly PriceSellApprovalService _service;

    public PriceSellApprovalController(AppDbContext db, PriceSellApprovalService service)
    {
        _db = db;
        _service = service;
    }

    // GET /PriceSellApproval
    [RequirePermission("pricesell_view_approval")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var username = HttpContext.Session.GetString("username") ?? "";
        var isAdmin = HttpContext.Session.GetString("is_admin") == "true";

        var query = _db.PriceSellApprovals.AsNoTracking().OrderByDescending(x => x.RequestedAt);
        if (!isAdmin)
            query = query.Where(x => x.Requester == username || x.Status == "PENDING")
                         .OrderByDescending(x => x.RequestedAt);

        return View(await query.Take(500).ToListAsync(ct));
    }

    // GET /PriceSellApproval/Details/5
    [RequirePermission("pricesell_view_approval")]
    public async Task<IActionResult> Details(long id, CancellationToken ct)
    {
        var item = await _db.PriceSellApprovals.AsNoTracking()
            .Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.ApprovalId == id, ct);
        if (item == null) return NotFound();

        ViewBag.Audit = await _db.PriceSellAudits.AsNoTracking()
            .Where(x => x.ApprovalId == id).OrderBy(x => x.PerformedAt).ToListAsync(ct);
        return View(item);
    }

    // Separate endpoints ensure each operation uses its own permission.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("pricesell_create")]
    public Task<IActionResult> SubmitCreate([FromBody] PriceSellApprovalSubmitDto request, CancellationToken ct)
        => SubmitCore(request, "CREATE", ct);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("pricesell_update")]
    public Task<IActionResult> SubmitUpdate([FromBody] PriceSellApprovalSubmitDto request, CancellationToken ct)
        => SubmitCore(request, "UPDATE", ct);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("pricesell_deactivate")]
    public Task<IActionResult> SubmitDeactivate([FromBody] PriceSellApprovalSubmitDto request, CancellationToken ct)
        => SubmitCore(request, "DEACTIVATE", ct);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("pricesell_import")]
    public Task<IActionResult> SubmitImport([FromBody] PriceSellApprovalSubmitDto request, CancellationToken ct)
        => SubmitCore(request, "IMPORT", ct);

    private async Task<IActionResult> SubmitCore(PriceSellApprovalSubmitDto request, string expectedType, CancellationToken ct)
    {
        if (!string.Equals(request?.RequestType?.Trim(), expectedType, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { success = false, message = $"Endpoint ini hanya menerima RequestType {expectedType}." });

        try
        {
            var username = HttpContext.Session.GetString("username") ?? "";
            var id = await _service.SubmitAsync(request, username, ct);
            return Ok(new { success = true, message = "Request approval berhasil diajukan.", approvalId = id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { success = false, message = "Request konflik dengan request pending lain atau data yang sudah ada." });
        }
    }

    // POST /PriceSellApproval/Approve/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("pricesell_approve")]
    public async Task<IActionResult> Approve(long id, [FromBody] PriceSellDecisionDto dto, CancellationToken ct)
    {
        try
        {
            await _service.ApproveAsync(id, HttpContext.Session.GetString("username") ?? "", dto?.Note, ct);
            return Ok(new { success = true, message = "Request disetujui dan perubahan berhasil diterapkan." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { success = false, message = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { success = false, message = "Gagal menerapkan perubahan; transaksi dibatalkan." }); }
    }

    // POST /PriceSellApproval/Reject/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("pricesell_reject")]
    public async Task<IActionResult> Reject(long id, [FromBody] PriceSellDecisionDto dto, CancellationToken ct)
    {
        try
        {
            await _service.RejectAsync(id, HttpContext.Session.GetString("username") ?? "", dto?.Note ?? "", ct);
            return Ok(new { success = true, message = "Request ditolak." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    // POST /PriceSellApproval/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("pricesell_cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        try
        {
            await _service.CancelAsync(id, HttpContext.Session.GetString("username") ?? "", ct);
            return Ok(new { success = true, message = "Request dibatalkan." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { success = false, message = ex.Message }); }
    }
}
