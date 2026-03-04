//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using TMSBilling.Data;
//using TMSBilling.Filters;
//using TMSBilling.Models;
//using TMSBilling.Services;

//namespace TMSBilling.Controllers
//{
//    /// <summary>
//    /// Developer-side: CRUD report definition, parameter config, permission, excel layout
//    /// Route: /ReportBuilder
//    /// </summary>
//    [SessionAuthorize]
//    public class ReportBuilderController : Controller
//    {
//        private readonly AppDbContext _db;
//        private readonly IExcelLayoutService _layoutSvc;

//        public ReportBuilderController(AppDbContext db, IExcelLayoutService layoutSvc)
//        {
//            _db = db;
//            _layoutSvc = layoutSvc;
//        }

//        // ─────────────────────────────────────────
//        // INDEX — list semua report definitions
//        // ─────────────────────────────────────────
//        public async Task<IActionResult> Index()
//        {
//            var list = await _db.ReportDefinitions
//                .OrderBy(r => r.category)
//                .ThenBy(r => r.report_name)
//                .ToListAsync();
//            return View(list);
//        }

//        // ─────────────────────────────────────────
//        // FORM — create / edit report definition
//        // ─────────────────────────────────────────
//        public async Task<IActionResult> Form(int? id)
//        {
//            var vm = new ReportBuilderFormVM();

//            if (id != null)
//            {
//                vm.Report = await _db.ReportDefinitions.FindAsync(id)
//                            ?? throw new Exception("Report not found.");

//                vm.Params = await _db.ReportParams
//                                .Where(p => p.report_id == id)
//                                .OrderBy(p => p.sort_order)
//                                .ToListAsync();

//                vm.Permissions = await _db.ReportPermissions
//                                     .Where(p => p.report_id == id)
//                                     .ToListAsync();

//                if (vm.Report.excel_layout_id.HasValue)
//                    vm.ExcelLayout = await _layoutSvc.LoadForFormAsync(vm.Report.excel_layout_id.Value);
//            }

//            await SetViewBags();
//            return View(vm);
//        }

//        // ─────────────────────────────────────────
//        // SAVE
//        // ─────────────────────────────────────────
//        [HttpPost]
//        public async Task<IActionResult> Save([FromBody] ReportBuilderFormVM vm)
//        {
//            var user = HttpContext.Session.GetString("username") ?? "System";

//            try
//            {
//                ReportDefinition report;

//                if (vm.Report.ID == 0)
//                {
//                    if (await _db.ReportDefinitions.AnyAsync(r => r.report_code == vm.Report.report_code))
//                        return Json(new { ok = false, message = "Report Code already exists." });

//                    report = vm.Report;
//                    report.entry_user = user;
//                    report.entry_date = DateTime.Now;
//                    _db.ReportDefinitions.Add(report);
//                    await _db.SaveChangesAsync();
//                }
//                else
//                {
//                    report = await _db.ReportDefinitions.FindAsync(vm.Report.ID)
//                             ?? throw new Exception("Report not found.");

//                    report.report_code = vm.Report.report_code;
//                    report.report_name = vm.Report.report_name;
//                    report.report_desc = vm.Report.report_desc;
//                    report.category = vm.Report.category;
//                    report.icon = vm.Report.icon;
//                    report.allowed_outputs = vm.Report.allowed_outputs;
//                    report.default_output = vm.Report.default_output;
//                    report.filename_template = vm.Report.filename_template;
//                    report.is_active = vm.Report.is_active;
//                    report.update_user = user;
//                    report.update_date = DateTime.Now;
//                }

//                int reportId = report.ID;

//                // Params (replace all)
//                _db.ReportParams.RemoveRange(
//                    _db.ReportParams.Where(p => p.report_id == reportId));
//                foreach (var (p, i) in vm.Params.Select((p, i) => (p, i)))
//                {
//                    p.ID = 0;
//                    p.report_id = reportId;
//                    p.sort_order = i;
//                    _db.ReportParams.Add(p);
//                }

//                // Permissions (replace all)
//                _db.ReportPermissions.RemoveRange(
//                    _db.ReportPermissions.Where(p => p.report_id == reportId));
//                foreach (var perm in vm.Permissions)
//                {
//                    perm.ID = 0;
//                    perm.report_id = reportId;
//                    _db.ReportPermissions.Add(perm);
//                }

//                await _db.SaveChangesAsync();
//                return Json(new { ok = true, id = reportId });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { ok = false, message = ex.Message });
//            }
//        }

//        // ─────────────────────────────────────────
//        // SAVE EXCEL LAYOUT — link layout ke report
//        // ─────────────────────────────────────────
//        [HttpPost]
//        public async Task<IActionResult> SaveLayout([FromBody] SaveBuilderLayoutRequest req)
//        {
//            try
//            {
//                // SaveFromFormAsync akan upsert ke RPT_EXCEL_LAYOUT
//                // layout_id di sini = report_id (1-to-1)
//                await _layoutSvc.SaveFromFormAsync(req.ReportId, req.Layout);

//                // Update excel_layout_id di ReportDefinition
//                var report = await _db.ReportDefinitions.FindAsync(req.ReportId)
//                             ?? throw new Exception("Report not found.");

//                var layout = await _db.MailReportExcelLayouts
//                    .FirstOrDefaultAsync(l => l.report_id == req.ReportId);

//                report.excel_layout_id = layout?.ID;
//                await _db.SaveChangesAsync();

//                return Json(new { ok = true });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { ok = false, message = ex.Message });
//            }
//        }

//        // ─────────────────────────────────────────
//        // GET EXCEL LAYOUT
//        // ─────────────────────────────────────────
//        [HttpGet]
//        public async Task<IActionResult> GetLayout(int reportId)
//        {
//            try
//            {
//                var vm = await _layoutSvc.LoadForFormAsync(reportId);
//                return Json(new { ok = true, data = vm });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { ok = false, message = ex.Message });
//            }
//        }

//        // ─────────────────────────────────────────
//        // DELETE
//        // ─────────────────────────────────────────
//        [HttpPost]
//        public async Task<IActionResult> Delete(int id)
//        {
//            var r = await _db.ReportDefinitions.FindAsync(id);
//            if (r == null) return NotFound();
//            _db.ReportDefinitions.Remove(r);
//            await _db.SaveChangesAsync();
//            return RedirectToAction("Index");
//        }

//        // ─────────────────────────────────────────
//        // TEST QUERY (shared dengan MailReport)
//        // ─────────────────────────────────────────
//        [HttpPost]
//        public async Task<IActionResult> TestQuery([FromBody] TestQueryRequest req)
//        {
//            // Delegate ke MailReportController logic yang sama
//            try
//            {
//                var rawSql = req.Sql.Trim().TrimEnd(';', ' ');
//                var upper = System.Text.RegularExpressions.Regex
//                    .Replace(rawSql.ToUpper(), @"\s+", " ").Trim();

//                var blocked = new[] { " INSERT ", " UPDATE ", " DELETE ", " DROP ",
//                                      " TRUNCATE ", " EXEC ", " EXECUTE ", " ALTER " };
//                if (blocked.Any(upper.Contains))
//                    return Json(new { ok = false, message = "Only SELECT queries allowed." });
//                if (!upper.StartsWith("SELECT") && !upper.StartsWith("WITH"))
//                    return Json(new { ok = false, message = "Only SELECT queries allowed." });

//                var sql = $"SELECT TOP 10 * FROM ({rawSql}) AS __test__";
//                if (upper.StartsWith("WITH")) sql = rawSql;

//                await using var conn = new Microsoft.Data.SqlClient.SqlConnection(
//                    _db.Database.GetConnectionString());
//                await conn.OpenAsync();
//                await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn)
//                { CommandTimeout = 15 };
//                await using var reader = await cmd.ExecuteReaderAsync();

//                var cols = Enumerable.Range(0, reader.FieldCount)
//                    .Select(i => reader.GetName(i)).ToList();
//                var rows = new List<Dictionary<string, string>>();
//                while (await reader.ReadAsync() && rows.Count < 10)
//                {
//                    var row = new Dictionary<string, string>();
//                    foreach (var col in cols) row[col] = reader[col]?.ToString() ?? "";
//                    rows.Add(row);
//                }
//                return Json(new { ok = true, columns = cols, rows });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { ok = false, message = ex.Message });
//            }
//        }

//        // ─────────────────────────────────────────
//        // RUN LOG
//        // ─────────────────────────────────────────
//        public async Task<IActionResult> Log(int? reportId)
//        {
//            var q = _db.ReportRunLogs
//                .Include(l => l.Report)
//                .AsQueryable();

//            if (reportId.HasValue) q = q.Where(l => l.report_id == reportId);

//            var logs = await q.OrderByDescending(l => l.run_at).Take(500).ToListAsync();
//            ViewBag.ReportId = reportId;
//            return View(logs);
//        }

//        // ─────────────────────────────────────────
//        private async Task SetViewBags()
//        {
//            ViewBag.Roles = await _db.Roles
//                .OrderBy(r => r.Name)
//                .Select(r => r.Name)
//                .ToListAsync();
//        }
//    }

//    public class SaveBuilderLayoutRequest
//    {
//        public int ReportId { get; set; }
//        public MailReportExcelLayoutVM Layout { get; set; } = new();
//    }
//}

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
    /// Developer-side: CRUD report definition, parameter config, permission, excel layout
    /// Route: /ReportBuilder
    /// </summary>
    [SessionAuthorize]
    public class ReportBuilderController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IExcelLayoutService _layoutSvc;

        public ReportBuilderController(AppDbContext db, IExcelLayoutService layoutSvc)
        {
            _db = db;
            _layoutSvc = layoutSvc;
        }

        // ─────────────────────────────────────────
        // INDEX — list semua report definitions
        // ─────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var list = await _db.ReportDefinitions
                .OrderBy(r => r.category)
                .ThenBy(r => r.report_name)
                .ToListAsync();
            return View(list);
        }

        // ─────────────────────────────────────────
        // FORM — create / edit report definition
        // ─────────────────────────────────────────
        public async Task<IActionResult> Form(int? id)
        {
            var vm = new ReportBuilderFormVM();

            if (id != null)
            {
                vm.Report = await _db.ReportDefinitions.FindAsync(id)
                            ?? throw new Exception("Report not found.");

                vm.Params = await _db.ReportParams
                                .Where(p => p.report_id == id)
                                .OrderBy(p => p.sort_order)
                                .ToListAsync();

                vm.Permissions = await _db.ReportPermissions
                                     .Where(p => p.report_id == id)
                                     .ToListAsync();

                if (vm.Report.excel_layout_id.HasValue)
                    vm.ExcelLayout = await _layoutSvc.LoadForFormAsync(vm.Report.excel_layout_id.Value, "reportbuilder");
            }

            await SetViewBags();
            return View(vm);
        }

        // ─────────────────────────────────────────
        // SAVE
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] ReportBuilderFormVM vm)
        {
            var user = HttpContext.Session.GetString("username") ?? "System";

            try
            {
                ReportDefinition report;

                if (vm.Report.ID == 0)
                {
                    if (await _db.ReportDefinitions.AnyAsync(r => r.report_code == vm.Report.report_code))
                        return Json(new { ok = false, message = "Report Code already exists." });

                    report = vm.Report;
                    report.entry_user = user;
                    report.entry_date = DateTime.Now;
                    _db.ReportDefinitions.Add(report);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    report = await _db.ReportDefinitions.FindAsync(vm.Report.ID)
                             ?? throw new Exception("Report not found.");

                    report.report_code = vm.Report.report_code;
                    report.report_name = vm.Report.report_name;
                    report.report_desc = vm.Report.report_desc;
                    report.category = vm.Report.category;
                    report.icon = vm.Report.icon;
                    report.allowed_outputs = vm.Report.allowed_outputs;
                    report.default_output = vm.Report.default_output;
                    report.filename_template = vm.Report.filename_template;
                    report.is_active = vm.Report.is_active;
                    report.update_user = user;
                    report.update_date = DateTime.Now;
                }

                int reportId = report.ID;

                // Params (replace all)
                _db.ReportParams.RemoveRange(
                    _db.ReportParams.Where(p => p.report_id == reportId));
                foreach (var (p, i) in vm.Params.Select((p, i) => (p, i)))
                {
                    p.ID = 0;
                    p.report_id = reportId;
                    p.sort_order = i;
                    _db.ReportParams.Add(p);
                }

                // Permissions (replace all)
                _db.ReportPermissions.RemoveRange(
                    _db.ReportPermissions.Where(p => p.report_id == reportId));
                foreach (var perm in vm.Permissions)
                {
                    perm.ID = 0;
                    perm.report_id = reportId;
                    _db.ReportPermissions.Add(perm);
                }

                await _db.SaveChangesAsync();
                return Json(new { ok = true, id = reportId });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // ─────────────────────────────────────────
        // SAVE EXCEL LAYOUT — link layout ke report
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> SaveLayout([FromBody] SaveBuilderLayoutRequest req)
        {
            try
            {
                // SaveFromFormAsync akan upsert ke RPT_EXCEL_LAYOUT
                // layout_id di sini = report_id (1-to-1)
                await _layoutSvc.SaveFromFormAsync(req.ReportId, req.Layout, "reportbuilder");

                // Update excel_layout_id di ReportDefinition
                var report = await _db.ReportDefinitions.FindAsync(req.ReportId)
                             ?? throw new Exception("Report not found.");

                var layout = await _db.MailReportExcelLayouts
                    .FirstOrDefaultAsync(l => l.report_id == req.ReportId);

                report.excel_layout_id = layout?.ID;
                await _db.SaveChangesAsync();

                return Json(new { ok = true });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // ─────────────────────────────────────────
        // GET EXCEL LAYOUT
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetLayout(int reportId)
        {
            try
            {
                var vm = await _layoutSvc.LoadForFormAsync(reportId, "reportbuilder");
                return Json(new { ok = true, data = vm });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // ─────────────────────────────────────────
        // DELETE
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _db.ReportDefinitions.FindAsync(id);
            if (r == null) return NotFound();
            _db.ReportDefinitions.Remove(r);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // ─────────────────────────────────────────
        // TEST QUERY (shared dengan MailReport)
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> TestQuery([FromBody] TestQueryRequest req)
        {
            // Delegate ke MailReportController logic yang sama
            try
            {
                var rawSql = req.Sql.Trim().TrimEnd(';', ' ');
                var upper = System.Text.RegularExpressions.Regex
                    .Replace(rawSql.ToUpper(), @"\s+", " ").Trim();

                var blocked = new[] { " INSERT ", " UPDATE ", " DELETE ", " DROP ",
                                      " TRUNCATE ", " EXEC ", " EXECUTE ", " ALTER " };
                if (blocked.Any(upper.Contains))
                    return Json(new { ok = false, message = "Only SELECT queries allowed." });
                if (!upper.StartsWith("SELECT") && !upper.StartsWith("WITH"))
                    return Json(new { ok = false, message = "Only SELECT queries allowed." });

                var sql = $"SELECT TOP 10 * FROM ({rawSql}) AS __test__";
                if (upper.StartsWith("WITH")) sql = rawSql;

                await using var conn = new Microsoft.Data.SqlClient.SqlConnection(
                    _db.Database.GetConnectionString());
                await conn.OpenAsync();
                await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn)
                { CommandTimeout = 15 };
                await using var reader = await cmd.ExecuteReaderAsync();

                var cols = Enumerable.Range(0, reader.FieldCount)
                    .Select(i => reader.GetName(i)).ToList();
                var rows = new List<Dictionary<string, string>>();
                while (await reader.ReadAsync() && rows.Count < 10)
                {
                    var row = new Dictionary<string, string>();
                    foreach (var col in cols) row[col] = reader[col]?.ToString() ?? "";
                    rows.Add(row);
                }
                return Json(new { ok = true, columns = cols, rows });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // ─────────────────────────────────────────
        // RUN LOG
        // ─────────────────────────────────────────
        public async Task<IActionResult> Log(int? reportId)
        {
            var q = _db.ReportRunLogs
                .Include(l => l.Report)
                .AsQueryable();

            if (reportId.HasValue) q = q.Where(l => l.report_id == reportId);

            var logs = await q.OrderByDescending(l => l.run_at).Take(500).ToListAsync();
            ViewBag.ReportId = reportId;
            return View(logs);
        }

        // ─────────────────────────────────────────
        private async Task SetViewBags()
        {
            ViewBag.Roles = await _db.Roles
                .OrderBy(r => r.Name)
                .Select(r => r.Name)
                .ToListAsync();
        }
    }

    public class SaveBuilderLayoutRequest
    {
        public int ReportId { get; set; }
        public MailReportExcelLayoutVM Layout { get; set; } = new();
    }
}