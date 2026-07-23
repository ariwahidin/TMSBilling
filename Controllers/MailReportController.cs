
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Services;

namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    [SuperAdminOnly]
    public class MailReportController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IMailReportService _mailer;
        private readonly IExcelLayoutService _excelLayout; // ← baru

        public MailReportController(
            AppDbContext db,
            IMailReportService mailer,
            IExcelLayoutService excelLayout) // ← baru
        {
            _db = db;
            _mailer = mailer;
            _excelLayout = excelLayout; // ← baru
        }

        // ─────────────────────────────────────────
        // INDEX
        // ─────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var list = await _db.MailReports
                .OrderBy(r => r.report_name)
                .ToListAsync();
            return View(list);
        }

        // ─────────────────────────────────────────
        // FORM
        // ─────────────────────────────────────────
        public async Task<IActionResult> Form(int? id)
        {
            var vm = new MailReportFormVM();

            if (id != null)
            {
                vm.Report = await _db.MailReports.FindAsync(id)
                            ?? throw new Exception("Report not found");

                vm.Template = await _db.MailReportTemplates
                                  .FirstOrDefaultAsync(t => t.report_id == id)
                              ?? new MailReportTemplate { report_id = id.Value, subject = "" };

                vm.Sections = await _db.MailReportSections
                                  .Where(s => s.report_id == id)
                                  .OrderBy(s => s.sort_order)
                                  .ToListAsync();

                vm.Recipients = await _db.MailReportRecipients
                                    .Include(r => r.TruckEmail)
                                    .Where(r => r.report_id == id)
                                    .ToListAsync();
            }

            SetDropdowns();
            return View(vm);
        }

        // ─────────────────────────────────────────
        // SAVE (tidak diubah)
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] MailReportFormVM vm)
        {
            var user = HttpContext.Session.GetString("username") ?? "System";

            try
            {
                MailReport report;

                if (vm.Report.ID == 0)
                {
                    if (await _db.MailReports.AnyAsync(r => r.report_code == vm.Report.report_code))
                        return Json(new { ok = false, message = "Report Code already exists." });

                    report = vm.Report;
                    report.entry_user = user;
                    report.entry_date = DateTime.Now;
                    _db.MailReports.Add(report);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    report = await _db.MailReports.FindAsync(vm.Report.ID)
                             ?? throw new Exception("Report not found");

                    report.report_code = vm.Report.report_code;
                    report.report_name = vm.Report.report_name;
                    report.report_desc = vm.Report.report_desc;
                    report.trigger_type = vm.Report.trigger_type;
                    report.event_key = vm.Report.event_key;
                    report.schedule_freq = vm.Report.schedule_freq;
                    report.schedule_time = vm.Report.schedule_time;
                    report.schedule_day_of_week = vm.Report.schedule_day_of_week;
                    report.schedule_day_of_month = vm.Report.schedule_day_of_month;
                    report.is_active = vm.Report.is_active;
                    report.update_user = user;
                    report.update_date = DateTime.Now;
                }

                int reportId = report.ID;

                // Template
                var tmpl = await _db.MailReportTemplates.FirstOrDefaultAsync(t => t.report_id == reportId);
                if (tmpl == null)
                {
                    tmpl = new MailReportTemplate { report_id = reportId, subject = "" };
                    _db.MailReportTemplates.Add(tmpl);
                }
                tmpl.subject = vm.Template.subject;
                tmpl.body_header = vm.Template.body_header;
                tmpl.body_footer = vm.Template.body_footer;
                tmpl.attach_pdf = vm.Template.attach_pdf;
                tmpl.attach_excel = vm.Template.attach_excel;
                tmpl.attach_filename = vm.Template.attach_filename;

                // Sections (replace all)
                _db.MailReportSections.RemoveRange(
                    _db.MailReportSections.Where(s => s.report_id == reportId));
                foreach (var sec in vm.Sections)
                {
                    sec.ID = 0;
                    sec.report_id = reportId;
                    _db.MailReportSections.Add(sec);
                }

                // Recipients (replace all)
                _db.MailReportRecipients.RemoveRange(
                    _db.MailReportRecipients.Where(r => r.report_id == reportId));
                foreach (var rec in vm.Recipients)
                {
                    rec.ID = 0;
                    rec.report_id = reportId;
                    _db.MailReportRecipients.Add(rec);
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
        // DELETE
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var rpt = await _db.MailReports.FindAsync(id);
            if (rpt == null) return NotFound();
            _db.MailReports.Remove(rpt);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // ─────────────────────────────────────────
        // PREVIEW
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Preview([FromBody] MailSendRequestVM req)
        {
            try
            {
                req.IsPreview = true;
                var preview = await _mailer.PreviewAsync(req.ReportId, req.EventParams);
                return Json(new { ok = true, data = preview });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // ─────────────────────────────────────────
        // SEND MANUAL
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> SendManual([FromBody] MailSendRequestVM req)
        {
            try
            {
                req.TriggeredBy = HttpContext.Session.GetString("username") ?? "Manual";
                var logId = await _mailer.SendAsync(req);
                return Json(new { ok = true, logId });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // ─────────────────────────────────────────
        // RETRIGGER
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Retrigger(int logId)
        {
            try
            {
                var user = HttpContext.Session.GetString("username") ?? "Manual";
                var newLogId = await _mailer.RetriggerAsync(logId, user);
                return Json(new { ok = true, logId = newLogId });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // ─────────────────────────────────────────
        // LOG
        // ─────────────────────────────────────────
        public async Task<IActionResult> Log(int? reportId)
        {
            var query = _db.MailReportLogs
                .Include(l => l.Report)
                .AsQueryable();

            if (reportId.HasValue)
                query = query.Where(l => l.report_id == reportId);

            var logs = await query
                .OrderByDescending(l => l.created_at)
                .Take(500)
                .ToListAsync();

            ViewBag.ReportId = reportId;
            ViewBag.ReportName = reportId.HasValue
                ? (await _db.MailReports.FindAsync(reportId))?.report_name
                : null;

            return View(logs);
        }

        // ─────────────────────────────────────────
        // LOG DETAIL
        // ─────────────────────────────────────────
        public async Task<IActionResult> LogDetail(int id)
        {
            var log = await _db.MailReportLogs
                .Include(l => l.Report)
                .FirstOrDefaultAsync(l => l.ID == id);

            if (log == null) return NotFound();
            return View(log);
        }

        // ─────────────────────────────────────────
        // TEST QUERY (tidak diubah)
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> TestQuery([FromBody] TestQueryRequest req)
        {
            try
            {
                var rawSql = req.Sql.Trim().TrimEnd(';', ' ');
                var upper = Regex.Replace(rawSql.ToUpper(), @"\s+", " ").Trim();

                var blocked = new[] { " INSERT ", " UPDATE ", " DELETE ", " DROP ",
                                      " TRUNCATE ", " EXEC ", " EXECUTE ", " ALTER " };
                if (blocked.Any(upper.Contains))
                    return Json(new { ok = false, message = "Only SELECT queries are allowed." });

                if (!upper.StartsWith("SELECT") && !upper.StartsWith("WITH"))
                    return Json(new { ok = false, message = "Only SELECT queries are allowed." });

                string sql;
                if (upper.StartsWith("WITH"))
                {
                    int depth = 0, lastOuterSelect = -1;
                    for (int i = 0; i < upper.Length - 6; i++)
                    {
                        if (upper[i] == '(') depth++;
                        else if (upper[i] == ')') depth--;
                        else if (depth == 0 && upper.Substring(i, 6) == "SELECT")
                        {
                            bool prevOk = i == 0 || !char.IsLetterOrDigit(upper[i - 1]);
                            bool nextOk = i + 6 >= upper.Length || !char.IsLetterOrDigit(upper[i + 6]);
                            if (prevOk && nextOk) lastOuterSelect = i;
                        }
                    }
                    sql = rawSql;
                }
                else
                {
                    sql = $"SELECT TOP 10 * FROM ({rawSql}) AS __test__";
                }

                using var conn = new Microsoft.Data.SqlClient.SqlConnection(
                    _db.Database.GetConnectionString());
                await conn.OpenAsync();
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn)
                { CommandTimeout = 15 };

                using var reader = await cmd.ExecuteReaderAsync();
                var cols = Enumerable.Range(0, reader.FieldCount)
                    .Select(i => reader.GetName(i)).ToList();

                var rows = new List<Dictionary<string, string>>();
                while (await reader.ReadAsync() && rows.Count < 10)
                {
                    var row = new Dictionary<string, string>();
                    foreach (var col in cols)
                        row[col] = reader[col]?.ToString() ?? "";
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
        // GET TRUCK EMAILS (tidak diubah)
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetTruckEmails(string? supCode)
        {
            var q = _db.VendorTruckEmails.Where(e => e.is_active == 1);
            if (!string.IsNullOrEmpty(supCode))
                q = q.Where(e => e.sup_code == supCode);

            var result = await q.Select(e => new
            {
                e.ID,
                e.sup_code,
                e.vehicle_no,
                e.email_address,
                e.email_name,
                e.email_type
            }).ToListAsync();

            return Json(result);
        }

        // ─────────────────────────────────────────
        // EXCEL LAYOUT — GET (baru)
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetExcelLayout(int reportId)
        {
            try
            {
                var vm = await _excelLayout.LoadForFormAsync(reportId);
                return Json(new { ok = true, data = vm });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // ─────────────────────────────────────────
        // EXCEL LAYOUT — SAVE (baru)
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> SaveExcelLayout([FromBody] SaveExcelLayoutRequest req)
        {
            try
            {
                await _excelLayout.SaveFromFormAsync(req.ReportId, req.Layout);
                return Json(new { ok = true });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // ─────────────────────────────────────────
        // Dropdowns
        // ─────────────────────────────────────────
        private void SetDropdowns()
        {
            ViewBag.ListVendor = _db.Vendors
                .Select(v => new SelectListItem
                {
                    Value = v.SUP_CODE,
                    Text = $"{v.SUP_CODE} - {v.SUP_NAME}"
                }).ToList();
        }
    }

    public class TestQueryRequest
    {
        public string Sql { get; set; } = "";
    }
}