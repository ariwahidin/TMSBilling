using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SelectPdf;
using TMSBilling.Data;
using TMSBilling.Models;

namespace TMSBilling.Services
{
    public interface IMailReportService
    {
        Task<MailPreviewVM> PreviewAsync(int reportId, Dictionary<string, string> eventParams);
        Task<int> SendAsync(MailSendRequestVM request);
        Task<int> RetriggerAsync(int logId, string triggeredBy);
        Task ProcessScheduledAsync();
        void TriggerEvent(string eventKey, Dictionary<string, string> eventParams, string triggeredBy);
    }

    public class MailReportService : IMailReportService
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;
        private readonly ILogger<MailReportService> _logger;
        private readonly string _connStr;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IExcelLayoutService _excelLayout;

        public MailReportService(
            AppDbContext db,
            IEmailService email,
            ILogger<MailReportService> logger,
            IConfiguration config,
            IServiceScopeFactory scopeFactory,
            IExcelLayoutService excelLayout)
        {
            _db = db;
            _email = email;
            _logger = logger;
            _connStr = config.GetConnectionString("DefaultConnection")!;
            _scopeFactory = scopeFactory;
            _excelLayout = excelLayout;
        }

        // ──────────────────────────────────────────────
        // PUBLIC: Fire-and-forget event trigger
        // ──────────────────────────────────────────────
        public void TriggerEvent(string eventKey, Dictionary<string, string> eventParams, string triggeredBy)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Buat scope baru — _db asli sudah di-dispose saat request selesai
                    using var scope = _scopeFactory.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<IMailReportService>();

                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var reports = await db.MailReports
                        .Where(r => r.trigger_type == "EVENT"
                                 && r.event_key == eventKey
                                 && r.is_active == 1)
                        .ToListAsync();

                    foreach (var rpt in reports)
                    {
                        await svc.SendAsync(new MailSendRequestVM
                        {
                            ReportId = rpt.ID,
                            EventParams = eventParams,
                            TriggeredBy = triggeredBy
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on TriggerEvent: {key}", eventKey);
                }
            });
        }

        // ──────────────────────────────────────────────
        // PUBLIC: Preview (no send, return rendered HTML)
        // ──────────────────────────────────────────────
        public async Task<MailPreviewVM> PreviewAsync(int reportId, Dictionary<string, string> eventParams)
        {
            var (report, template, sections, recipients) = await LoadReportAsync(reportId);

            var (subject, bodyHtml, placeholders) = await RenderAsync(report, template, sections, eventParams);

            return new MailPreviewVM
            {
                Subject = subject,
                BodyHtml = bodyHtml,
                RecipientsTo = recipients.Where(r => r.email_type == "TO" && r.is_active == 1)
                                         .Select(r => FormatEmail(r)).ToList(),
                RecipientsCC = recipients.Where(r => r.email_type == "CC" && r.is_active == 1)
                                         .Select(r => FormatEmail(r)).ToList(),
                HasPdf = template.attach_pdf == 1,
                HasExcel = template.attach_excel == 1
            };
        }

        // ──────────────────────────────────────────────
        // PUBLIC: Send (returns log ID)
        // ──────────────────────────────────────────────
        public async Task<int> SendAsync(MailSendRequestVM request)
        {
            var (report, template, sections, recipients) = await LoadReportAsync(request.ReportId);

            var log = new MailReportLog
            {
                report_id = report.ID,
                trigger_type = request.IsPreview ? "PREVIEW" : report.trigger_type,
                trigger_ref = string.Join(";", request.EventParams.Select(k => $"{k.Key}={k.Value}")),
                status = "PENDING",
                triggered_by = request.TriggeredBy,
                created_at = DateTime.Now
            };
            _db.MailReportLogs.Add(log);
            await _db.SaveChangesAsync();

            try
            {
                var (subject, bodyHtml, _) = await RenderAsync(report, template, sections, request.EventParams);

                var toList = recipients
                    .Where(r => r.email_type == "TO" && r.is_active == 1)
                    .Select(FormatEmail)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();
                var ccList = recipients
                    .Where(r => r.email_type == "CC" && r.is_active == 1)
                    .Select(FormatEmail)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();
                var bccList = recipients
                    .Where(r => r.email_type == "BCC" && r.is_active == 1)
                    .Select(FormatEmail)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();

                // Inject email_to / email_cc dari EventParams (tanpa ubah service signature)
                if (request.EventParams.TryGetValue("email_to", out var extraTo))
                    foreach (var addr in extraTo.Split(',', ';'))
                        if (!string.IsNullOrWhiteSpace(addr)) toList.Add(addr.Trim());

                if (request.EventParams.TryGetValue("email_cc", out var extraCc))
                    foreach (var addr in extraCc.Split(',', ';'))
                        if (!string.IsNullOrWhiteSpace(addr)) ccList.Add(addr.Trim());

                // Guard: tidak bisa kirim kalau tidak ada TO/CC/BCC sama sekali
                if (!toList.Any() && !ccList.Any() && !bccList.Any())
                    throw new InvalidOperationException(
                        $"Report '{report.report_name}' tidak memiliki recipient aktif. " +
                        $"Total recipient di DB: {recipients.Count}. " +
                        $"Pastikan recipient sudah ditambahkan dan is_active = 1.");

                log.subject_sent = subject;
                log.body_sent = bodyHtml;
                log.recipients_to = string.Join(";", toList);
                log.recipients_cc = string.Join(";", ccList);

                // Build attachments
                var attachments = new List<EmailAttachment>();

                //if (template.attach_excel == 1)
                //{
                //    var excelBytes = await BuildExcelAsync(sections, request.EventParams);
                //    var fname = ResolvePlaceholders(template.attach_filename ?? "Report_{{date}}", request.EventParams)
                //                    .Replace("{{date}}", DateTime.Now.ToString("yyyyMMdd"));
                //    attachments.Add(new EmailAttachment
                //    {
                //        FileName = fname.EndsWith(".xlsx") ? fname : fname + ".xlsx",
                //        ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                //        Data = excelBytes
                //    });
                //}

                if (template.attach_excel == 1)
                {
                    // Pilih builder: custom layout (baru) atau default (lama)
                    byte[] excelBytes = await _excelLayout.HasCustomLayoutAsync(request.ReportId)
                        ? await _excelLayout.BuildAsync(request.ReportId, request.EventParams)
                        : await BuildExcelAsync(sections, request.EventParams);

                    var fname = ResolvePlaceholders(
                        template.attach_filename ?? "Report_{{date}}",
                        request.EventParams)
                        .Replace("{{date}}", DateTime.Now.ToString("yyyyMMdd"));

                    attachments.Add(new EmailAttachment
                    {
                        FileName = fname.EndsWith(".xlsx") ? fname : fname + ".xlsx",
                        ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        Data = excelBytes
                    });
                }

                if (template.attach_pdf == 1)
                {
                    var pdfBytes = BuildPdf(bodyHtml);
                    var fname = ResolvePlaceholders(template.attach_filename ?? "Report_{{date}}", request.EventParams)
                                    .Replace("{{date}}", DateTime.Now.ToString("yyyyMMdd"));
                    attachments.Add(new EmailAttachment
                    {
                        FileName = fname.EndsWith(".pdf") ? fname : fname + ".pdf",
                        ContentType = "application/pdf",
                        Data = pdfBytes
                    });
                }

                await _email.SendAsync(new EmailMessage
                {
                    To = toList,
                    CC = ccList,
                    BCC = bccList,
                    Subject = subject,
                    Body = bodyHtml,
                    IsHtml = true,
                    Attachments = attachments
                });

                log.status = "SENT";
                log.sent_at = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MailReport send failed for report {id}", request.ReportId);
                log.status = "FAILED";
                log.error_message = ex.Message;
            }

            await _db.SaveChangesAsync();
            return log.ID;
        }

        // ──────────────────────────────────────────────
        // PUBLIC: Retrigger from history log
        // ──────────────────────────────────────────────
        public async Task<int> RetriggerAsync(int logId, string triggeredBy)
        {
            var oldLog = await _db.MailReportLogs.FindAsync(logId)
                         ?? throw new Exception($"Log {logId} not found");

            // Parse event params dari trigger_ref lama
            var eventParams = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(oldLog.trigger_ref))
            {
                foreach (var pair in oldLog.trigger_ref.Split(';'))
                {
                    var kv = pair.Split('=', 2);
                    if (kv.Length == 2) eventParams[kv[0]] = kv[1];
                }
            }

            oldLog.retry_count++;
            await _db.SaveChangesAsync();

            return await SendAsync(new MailSendRequestVM
            {
                ReportId = oldLog.report_id,
                EventParams = eventParams,
                TriggeredBy = triggeredBy + " (retrigger)"
            });
        }

        // ──────────────────────────────────────────────
        // PUBLIC: Process all due scheduled reports
        // ──────────────────────────────────────────────
        public async Task ProcessScheduledAsync()
        {
            var now = DateTime.Now;

            var reports = await _db.MailReports
                .Where(r => r.trigger_type == "SCHEDULED" && r.is_active == 1)
                .ToListAsync();

            foreach (var rpt in reports)
            {
                if (!IsDueNow(rpt, now)) continue;

                await SendAsync(new MailSendRequestVM
                {
                    ReportId = rpt.ID,
                    EventParams = new Dictionary<string, string>
                    {
                        ["date"] = now.ToString("yyyy-MM-dd"),
                        ["date_label"] = now.ToString("dd MMMM yyyy")
                    },
                    TriggeredBy = "SCHEDULER"
                });
            }
        }

        // ──────────────────────────────────────────────
        // PRIVATE: Load report with all relations
        // ──────────────────────────────────────────────
        private async Task<(MailReport, MailReportTemplate, List<MailReportSection>, List<MailReportRecipient>)>
            LoadReportAsync(int reportId)
        {
            var report = await _db.MailReports.FindAsync(reportId)
                         ?? throw new Exception($"Report {reportId} not found");

            var template = await _db.MailReportTemplates
                               .FirstOrDefaultAsync(t => t.report_id == reportId)
                           ?? throw new Exception($"Template not found for report {reportId}");

            var sections = await _db.MailReportSections
                               .Where(s => s.report_id == reportId)
                               .OrderBy(s => s.sort_order)
                               .ToListAsync();

            var recipients = await _db.MailReportRecipients
                               .Include(r => r.TruckEmail)
                               .Where(r => r.report_id == reportId && r.is_active == 1)
                               .ToListAsync();

            return (report, template, sections, recipients);
        }

        // ──────────────────────────────────────────────
        // PRIVATE: Render subject + full HTML body
        // ──────────────────────────────────────────────
        private async Task<(string subject, string bodyHtml, Dictionary<string, string> placeholders)>
            RenderAsync(MailReport report, MailReportTemplate template,
                        List<MailReportSection> sections, Dictionary<string, string> eventParams)
        {
            var placeholders = new Dictionary<string, string>(eventParams, StringComparer.OrdinalIgnoreCase);

            // Add common placeholders
            placeholders["date"] = DateTime.Now.ToString("yyyy-MM-dd");
            placeholders["date_label"] = DateTime.Now.ToString("dd MMMM yyyy");
            placeholders["report_name"] = report.report_name;

            var bodySb = new StringBuilder();
            bodySb.AppendLine(EmailBaseHtml.Open());

            // Header section
            if (!string.IsNullOrWhiteSpace(template.body_header))
                bodySb.AppendLine(ResolvePlaceholders(template.body_header, placeholders));

            // Query sections
            foreach (var sec in sections)
            {
                var data = await RunSectionQueryAsync(sec, placeholders);

                // If section is used as placeholder source, populate from first row
                if (sec.use_as_placeholder == 1 && data.Rows.Count > 0)
                {
                    foreach (DataColumn col in data.Columns)
                    {
                        placeholders[col.ColumnName] = FormatCellValue(data.Rows[0][col.ColumnName], col);
                    }
                }

                bodySb.AppendLine($"<div class='section'>");
                bodySb.AppendLine($"<div class='section-title'>{sec.section_label}</div>");
                bodySb.AppendLine(RenderSection(sec, data));
                bodySb.AppendLine("</div>");
            }

            // Footer section
            if (!string.IsNullOrWhiteSpace(template.body_footer))
                bodySb.AppendLine(ResolvePlaceholders(template.body_footer, placeholders));

            bodySb.AppendLine(EmailBaseHtml.Close());

            var finalHtml = ResolvePlaceholders(bodySb.ToString(), placeholders);
            var finalSubject = ResolvePlaceholders(template.subject, placeholders);

            return (finalSubject, finalHtml, placeholders);
        }

        // ──────────────────────────────────────────────
        // PRIVATE: Execute a section's SQL query safely
        // ──────────────────────────────────────────────
        private async Task<DataTable> RunSectionQueryAsync(
            MailReportSection sec, Dictionary<string, string> placeholders)
        {
            var dt = new DataTable();
            if (string.IsNullOrWhiteSpace(sec.sql_query)) return dt;

            // Build full query — inject WHERE if provided
            var sql = sec.sql_query.Trim();

            if (!string.IsNullOrWhiteSpace(sec.sql_where))
            {
                var where = ResolvePlaceholders(sec.sql_where, placeholders);

                // Cek apakah query asli sudah punya WHERE clause
                // Hapus trailing semicolon dulu
                sql = sql.TrimEnd(';', ' ');

                // Deteksi apakah sudah ada WHERE di query (di luar subquery)
                // Cara aman: cek kata WHERE setelah FROM terakhir di level paling luar
                var upperSql = sql.ToUpper();

                // Hitung kedalaman parenthesis untuk cari WHERE di level luar
                bool hasOuterWhere = false;
                int depth = 0;
                int whereIdx = -1;
                for (int i = 0; i < upperSql.Length - 5; i++)
                {
                    if (upperSql[i] == '(') depth++;
                    else if (upperSql[i] == ')') depth--;
                    else if (depth == 0 && upperSql.Substring(i, 5) == "WHERE")
                    {
                        // Pastikan bukan bagian dari nama kolom
                        bool prevOk = i == 0 || !char.IsLetterOrDigit(upperSql[i - 1]);
                        bool nextOk = i + 5 >= upperSql.Length || !char.IsLetterOrDigit(upperSql[i + 5]);
                        if (prevOk && nextOk) { hasOuterWhere = true; whereIdx = i; break; }
                    }
                }

                sql = hasOuterWhere
                    ? sql + $" AND ({where})"
                    : sql + $" WHERE {where}";
            }

            // Security: ensure no DML
            if (!IsSafeSelectQuery(sql))
                throw new Exception($"Section '{sec.section_label}': Only SELECT queries are allowed.");

            try
            {
                await using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };

                // Bind @param style params from placeholders
                foreach (var kv in placeholders)
                {
                    var pname = "@" + kv.Key;
                    if (sql.Contains(pname))
                        cmd.Parameters.AddWithValue(pname, kv.Value);
                }

                var adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Query error in section {label}", sec.section_label);
                dt.Columns.Add("Error");
                dt.Rows.Add(ex.Message);
            }

            return dt;
        }

        // ──────────────────────────────────────────────
        // PRIVATE: Format cell value — handle DateTime nicely
        // ──────────────────────────────────────────────
        private string FormatCellValue(object? val, DataColumn col)
        {
            if (val == null || val == DBNull.Value) return "";

            if (col.DataType == typeof(DateTime))
            {
                var dt = (DateTime)val;
                // Kalau time component = midnight → tampilkan date only
                return dt.TimeOfDay == TimeSpan.Zero
                    ? dt.ToString("yyyy-MM-dd")
                    : dt.ToString("yyyy-MM-dd HH:mm");
            }

            if (col.DataType == typeof(TimeSpan))
                return ((TimeSpan)val).ToString(@"hh\:mm\:ss");

            return val.ToString() ?? "";
        }

        // ──────────────────────────────────────────────
        // PRIVATE: Render DataTable → HTML
        // ──────────────────────────────────────────────
        private string RenderSection(MailReportSection sec, DataTable dt)
        {
            if (dt.Rows.Count == 0)
                return "<p class='no-data'>Tidak ada data.</p>";

            // Parse visible columns — strip brackets, case-insensitive match ke nama kolom di DataTable
            List<string> visibleCols;
            if (string.IsNullOrWhiteSpace(sec.visible_columns))
            {
                visibleCols = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            }
            else
            {
                var requested = sec.visible_columns
                    .Split(',')
                    .Select(c => c.Trim().Trim('[', ']'))  // strip bracket kalau ada
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();

                // Match case-insensitive ke nama kolom aktual di DataTable
                var actualCols = dt.Columns.Cast<DataColumn>()
                    .ToDictionary(c => c.ColumnName.ToLower(), c => c.ColumnName);

                visibleCols = requested
                    .Select(r => actualCols.TryGetValue(r.ToLower(), out var actual) ? actual : r)
                    .ToList();
            }

            // ── KEY_VALUE layout ──
            if (sec.display_mode == "KEY_VALUE" || sec.display_mode == "SINGLE_VALUE")
            {
                var sb = new StringBuilder("<table class='kv-table'>");
                foreach (DataColumn col in dt.Columns)
                {
                    if (!visibleCols.Contains(col.ColumnName)) continue;
                    var val = FormatCellValue(dt.Rows[0][col.ColumnName], col);
                    sb.Append($"<tr><th>{col.ColumnName}</th><td>{val}</td></tr>");
                }
                sb.Append("</table>");
                return sb.ToString();
            }

            // ── TABLE layout ──
            var numericTypes = new[]
            {
                typeof(int), typeof(long), typeof(short), typeof(byte),
                typeof(decimal), typeof(double), typeof(float),
                typeof(int?), typeof(long?), typeof(decimal?), typeof(double?), typeof(float?)
            };

            // Pre-compute which visible cols are numeric
            var numericCols = visibleCols
                .Where(c => dt.Columns.Contains(c) &&
                            numericTypes.Contains(dt.Columns[c]!.DataType))
                .ToHashSet();

            // Compute column totals
            var totals = new Dictionary<string, decimal>();
            if (sec.show_grand_total == 1)
            {
                foreach (var col in numericCols)
                {
                    totals[col] = dt.AsEnumerable()
                        .Sum(r => r[col] == DBNull.Value ? 0m : Convert.ToDecimal(r[col]));
                }
            }

            var html = new StringBuilder();
            html.Append("<table class='data-table'>");

            // thead
            html.Append("<thead><tr>");
            foreach (var col in visibleCols)
                html.Append($"<th>{col}</th>");
            html.Append("</tr></thead>");

            // tbody
            html.Append("<tbody>");
            foreach (DataRow row in dt.Rows)
            {
                html.Append("<tr>");
                foreach (var col in visibleCols)
                {
                    var colDef = dt.Columns.Contains(col) ? dt.Columns[col]! : null;
                    var val = colDef != null ? FormatCellValue(row[col], colDef) : "";
                    var align = numericCols.Contains(col) ? " style='text-align:right;'" : "";
                    html.Append($"<td{align}>{val}</td>");
                }
                html.Append("</tr>");
            }
            html.Append("</tbody>");

            // tfoot Grand Total
            if (sec.show_grand_total == 1 && totals.Any())
            {
                var label = sec.grand_total_label ?? "Grand Total";
                var firstCol = true;
                var labelPlaced = false;

                html.Append("<tfoot><tr>");
                foreach (var col in visibleCols)
                {
                    if (firstCol)
                    {
                        // Kolom pertama: label Grand Total, colspan kalau bukan numerik
                        if (!numericCols.Contains(col))
                        {
                            // Hitung berapa kolom non-numerik di depan sebelum numerik pertama
                            int colspan = 0;
                            foreach (var c in visibleCols)
                            {
                                if (!numericCols.Contains(c)) colspan++;
                                else break;
                            }
                            html.Append($"<td colspan='{colspan}' style='text-align:right;font-weight:bold;background:#fffde7;'>{label}</td>");
                            labelPlaced = true;
                            firstCol = false;
                            // Skip non-numeric cols yang sudah di-colspan
                            var skipped = 0;
                            foreach (var c in visibleCols)
                            {
                                if (!numericCols.Contains(c)) skipped++;
                                else break;
                            }
                            // lanjut render dari kolom numerik pertama
                            foreach (var c in visibleCols.Skip(skipped))
                            {
                                if (numericCols.Contains(c))
                                {
                                    var total = totals.TryGetValue(c, out var t) ? t : 0;
                                    var fmt = total == Math.Floor(total)
                                        ? total.ToString("N0")
                                        : total.ToString("N2");
                                    html.Append($"<td style='text-align:right;font-weight:bold;background:#fffde7;'>{fmt}</td>");
                                }
                                else
                                {
                                    html.Append("<td style='background:#fffde7;'></td>");
                                }
                            }
                            break;
                        }
                        else
                        {
                            // Kolom pertama sudah numerik — label di sini
                            html.Append($"<td style='text-align:right;font-weight:bold;background:#fffde7;'>{label}</td>");
                            labelPlaced = true;
                            firstCol = false;
                        }
                    }
                    else if (!labelPlaced)
                    {
                        html.Append($"<td style='text-align:right;font-weight:bold;background:#fffde7;'>{label}</td>");
                        labelPlaced = true;
                    }
                    else
                    {
                        if (numericCols.Contains(col))
                        {
                            var total = totals.TryGetValue(col, out var t) ? t : 0;
                            var fmt = total == Math.Floor(total)
                                ? total.ToString("N0")
                                : total.ToString("N2");
                            html.Append($"<td style='text-align:right;font-weight:bold;background:#fffde7;'>{fmt}</td>");
                        }
                        else
                        {
                            html.Append("<td style='background:#fffde7;'></td>");
                        }
                    }
                }
                html.Append("</tr></tfoot>");
            }

            html.Append("</table>");
            return html.ToString();
        }

        // ──────────────────────────────────────────────
        // PRIVATE: Build Excel attachment
        // ──────────────────────────────────────────────
        private async Task<byte[]> BuildExcelAsync(
            List<MailReportSection> sections, Dictionary<string, string> eventParams)
        {
            using var wb = new XLWorkbook();

            foreach (var sec in sections)
            {
                var dt = await RunSectionQueryAsync(sec, eventParams);
                var ws = wb.Worksheets.Add(sec.section_label.Length > 31
                    ? sec.section_label[..31] : sec.section_label);

                // Header row
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    var cell = ws.Cell(1, i + 1);
                    cell.Value = dt.Columns[i].ColumnName;
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
                    cell.Style.Font.FontColor = XLColor.White;
                }

                // Data rows
                for (int r = 0; r < dt.Rows.Count; r++)
                    for (int c = 0; c < dt.Columns.Count; c++)
                        ws.Cell(r + 2, c + 1).Value = dt.Rows[r][c]?.ToString() ?? "";

                ws.Columns().AdjustToContents();
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        // ──────────────────────────────────────────────
        // PRIVATE: Build PDF from HTML
        // ──────────────────────────────────────────────
        private byte[] BuildPdf(string html)
        {
            var converter = new HtmlToPdf();
            converter.Options.PdfPageSize = PdfPageSize.A4;
            converter.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
            converter.Options.MarginTop = 20;
            converter.Options.MarginBottom = 20;
            converter.Options.MarginLeft = 20;
            converter.Options.MarginRight = 20;

            var doc = converter.ConvertHtmlString(html);
            using var ms = new MemoryStream();
            doc.Save(ms);
            doc.Close();
            return ms.ToArray();
        }

        // ──────────────────────────────────────────────
        // PRIVATE: Helpers
        // ──────────────────────────────────────────────
        private string ResolvePlaceholders(string template, Dictionary<string, string> values)
        {
            return Regex.Replace(template, @"\{\{(\w+)\}\}", m =>
                values.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);
        }

        private bool IsSafeSelectQuery(string sql)
        {
            var normalized = Regex.Replace(sql.ToUpper().Trim(), @"\s+", " ");
            // Must start with SELECT or WITH (CTE)
            if (!normalized.StartsWith("SELECT") && !normalized.StartsWith("WITH"))
                return false;
            // Block DML keywords
            var blocked = new[] { " INSERT ", " UPDATE ", " DELETE ", " DROP ", " TRUNCATE ", " EXEC ", " EXECUTE ", " ALTER ", " CREATE " };
            return !blocked.Any(normalized.Contains);
        }

        private bool IsDueNow(MailReport rpt, DateTime now)
        {
            if (!rpt.schedule_time.HasValue) return false;

            var schedHour = rpt.schedule_time.Value.Hours;
            var schedMin = rpt.schedule_time.Value.Minutes;

            // Allow ±5 min window
            var diff = Math.Abs((now.Hour * 60 + now.Minute) - (schedHour * 60 + schedMin));
            if (diff > 5) return false;

            return rpt.schedule_freq switch
            {
                "DAILY" => true,
                "WEEKLY" => rpt.schedule_day_of_week.HasValue && (int)now.DayOfWeek == rpt.schedule_day_of_week,
                "MONTHLY" => rpt.schedule_day_of_month.HasValue && now.Day == rpt.schedule_day_of_month,
                _ => false
            };
        }

        private string FormatEmail(MailReportRecipient r)
        {
            // TRUCK_EMAIL source — gunakan data dari TruckEmail jika loaded,
            // fallback ke email_address yang sudah di-snapshot saat add recipient
            if (r.recipient_source == "TRUCK_EMAIL")
            {
                if (r.TruckEmail != null)
                    return string.IsNullOrEmpty(r.TruckEmail.email_name)
                        ? r.TruckEmail.email_address
                        : $"{r.TruckEmail.email_name} <{r.TruckEmail.email_address}>";

                // TruckEmail tidak di-Include, pakai snapshot
                return string.IsNullOrEmpty(r.email_name)
                    ? r.email_address ?? ""
                    : $"{r.email_name} <{r.email_address}>";
            }

            // MANUAL
            return string.IsNullOrEmpty(r.email_name)
                ? r.email_address ?? ""
                : $"{r.email_name} <{r.email_address}>";
        }
    }

    // ──────────────────────────────────────────────
    // Base HTML wrapper for email body
    // ──────────────────────────────────────────────
    public static class EmailBaseHtml
    {
        public static string Open() => @"
<!DOCTYPE html>
<html lang='id'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<style>
  * { margin:0; padding:0; box-sizing:border-box; }
  body {
    background-color: #f0f2f5;
    font-family: 'Segoe UI', Arial, sans-serif;
    font-size: 13px;
    color: #2d3748;
    -webkit-font-smoothing: antialiased;
  }
  .email-wrapper {
    max-width: 680px;
    margin: 24px auto;
    background: #ffffff;
    border-radius: 8px;
    overflow: hidden;
    box-shadow: 0 2px 8px rgba(0,0,0,0.08);
  }
  /* ── Header bar ── */
  .email-header {
    background: linear-gradient(135deg, #1a56db 0%, #1e429f 100%);
    padding: 28px 36px;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }
  .email-header .brand {
    color: #ffffff;
    font-size: 18px;
    font-weight: 700;
    letter-spacing: 0.3px;
  }
  .email-header .brand span {
    opacity: 0.7;
    font-weight: 400;
    font-size: 13px;
    display: block;
    margin-top: 2px;
  }
  .email-header .date-badge {
    background: rgba(255,255,255,0.15);
    color: #fff;
    font-size: 11px;
    padding: 4px 10px;
    border-radius: 20px;
    letter-spacing: 0.3px;
  }
  /* ── Body content ── */
  .email-body {
    padding: 32px 36px;
  }
  /* ── Section ── */
  .section {
    margin-bottom: 28px;
  }
  .section-title {
    font-size: 10px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: #1a56db;
    padding-bottom: 8px;
    margin-bottom: 12px;
    border-bottom: 2px solid #e8edf8;
    display: flex;
    align-items: center;
    gap: 6px;
  }
  .section-title::before {
    content: '';
    display: inline-block;
    width: 4px;
    height: 14px;
    background: #1a56db;
    border-radius: 2px;
  }
  /* ── Data table ── */
  .data-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 12px;
  }
  .data-table thead tr {
    background: #1a56db;
  }
  .data-table thead th {
    color: #ffffff;
    padding: 9px 12px;
    text-align: left;
    font-size: 11px;
    font-weight: 600;
    letter-spacing: 0.04em;
    white-space: nowrap;
  }
  .data-table tbody tr {
    border-bottom: 1px solid #edf0f7;
    transition: background 0.1s;
  }
  .data-table tbody tr:nth-child(even) {
    background: #f7f9fc;
  }
  .data-table tbody td {
    padding: 8px 12px;
    color: #374151;
    vertical-align: middle;
  }
  .data-table tbody tr:last-child {
    border-bottom: none;
  }
  /* ── Key-value table ── */
  .kv-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 12px;
  }
  .kv-table tr {
    border-bottom: 1px solid #edf0f7;
  }
  .kv-table tr:last-child { border-bottom: none; }
  .kv-table th {
    background: #f7f9fc;
    color: #6b7280;
    font-weight: 600;
    padding: 8px 14px;
    text-align: left;
    width: 200px;
    font-size: 11px;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    white-space: nowrap;
  }
  .kv-table td {
    padding: 8px 14px;
    color: #1f2937;
    font-weight: 500;
  }
  /* ── No data ── */
  .no-data {
    text-align: center;
    color: #9ca3af;
    font-style: italic;
    padding: 20px;
    font-size: 12px;
    background: #fafafa;
    border-radius: 4px;
    border: 1px dashed #e5e7eb;
  }
  /* ── Prose text (header/footer html) ── */
  p { margin: 0 0 8px 0; line-height: 1.7; color: #374151; }
  strong { color: #1f2937; }
  a { color: #1a56db; text-decoration: none; }
  ul, ol { padding-left: 20px; margin: 8px 0; }
  li { margin-bottom: 4px; line-height: 1.6; }
  /* ── Footer ── */
  .email-footer {
    background: #f7f9fc;
    border-top: 1px solid #e8edf8;
    padding: 20px 36px;
    font-size: 11px;
    color: #9ca3af;
    line-height: 1.6;
  }
  .email-footer strong { color: #6b7280; }
  .divider {
    border: none;
    border-top: 1px solid #e8edf8;
    margin: 20px 0;
  }
</style>
</head>
<body>
<div class='email-wrapper'>
  <div class='email-header-a'>
  </div>
  <div class='email-body'>";

        public static string Close() => @"
  </div>
  <div class='email-footer-a'>
  </div>
</div>
</body>
</html>";
    }

    // ──────────────────────────────────────────────
    // Email message model (adapt to your existing IEmailService)
    // ──────────────────────────────────────────────
    public class EmailAttachment
    {
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    public class EmailMessage
    {
        public List<string> To { get; set; } = new();
        public List<string> CC { get; set; } = new();
        public List<string> BCC { get; set; } = new();
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsHtml { get; set; } = true;
        public List<EmailAttachment> Attachments { get; set; } = new();
    }
}