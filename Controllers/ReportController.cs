using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Services;

namespace TMSBilling.Controllers
{
    /// <summary>
    /// User-side: browse report list, isi parameter, generate & download
    /// Route: /Report
    /// </summary>
    [SessionAuthorize]
    public class ReportController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IReportRunnerService _runner;

        public ReportController(AppDbContext db, IReportRunnerService runner)
        {
            _db = db;
            _runner = runner;
        }

        // ─────────────────────────────────────────
        // INDEX — list report yang boleh diakses user
        // ─────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var username = HttpContext.Session.GetString("username") ?? "";
            var roleName = HttpContext.Session.GetString("role") ?? "";
            var userId = HttpContext.Session.GetInt32("user_id");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var reports = await _runner.GetAccessibleReportsAsync(userId.Value);
            //var reports = await _runner.GetAccessibleReportsAsync(username, roleName);

            // Group by category untuk tampilan UI
            var grouped = reports
                .GroupBy(r => r.category ?? "General")
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());

            return View(grouped);
        }

        // ─────────────────────────────────────────
        // GENERATE — tampilkan form parameter
        // ─────────────────────────────────────────
        public async Task<IActionResult> Generate(int id)
        {
            var report = await _db.ReportDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ID == id && r.is_active == 1)
                ?? throw new Exception("Report not found.");

            var paramDefs = await _db.ReportParams
                .AsNoTracking()
                .Where(p => p.report_id == id && p.is_hidden == 0)
                .OrderBy(p => p.sort_order)
                .ToListAsync();

            ViewBag.Report = report;
            ViewBag.ParamDefs = paramDefs;
            return View();
        }

        // ─────────────────────────────────────────
        // DOWNLOAD — generate & stream file
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Download([FromBody] ReportGenerateRequest req)
        {
            var username = HttpContext.Session.GetString("username") ?? "anonymous";

            try
            {
                var result = await _runner.RunAsync(req, username);
                return File(result.Data, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // ─────────────────────────────────────────
        // GET PARAM OPTIONS — untuk dropdown dinamis
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> GetParamOptions([FromBody] ParamOptionsRequest req)
        {
            try
            {
                var opts = await _runner.GetParamOptionsAsync(
                    req.ReportId, req.ParamKey, req.CurrentValues);
                return Json(new { ok = true, data = opts });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // ─────────────────────────────────────────
        // MY HISTORY — log run user ini
        // ─────────────────────────────────────────
        public async Task<IActionResult> History(int? reportId)
        {
            var username = HttpContext.Session.GetString("username") ?? "";

            var q = _db.ReportRunLogs
                .Include(l => l.Report)
                .Where(l => l.run_by == username)
                .AsQueryable();

            if (reportId.HasValue) q = q.Where(l => l.report_id == reportId);

            var logs = await q.OrderByDescending(l => l.run_at).Take(100).ToListAsync();
            ViewBag.ReportId = reportId;
            return View(logs);
        }
    }

    public class ParamOptionsRequest
    {
        public int ReportId { get; set; }
        public string ParamKey { get; set; } = "";
        public Dictionary<string, string> CurrentValues { get; set; } = new();
    }
}