using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TMSBilling.Data;
using TMSBilling.Models;

namespace TMSBilling.Services
{
    // ══════════════════════════════════════════════════════════════
    // Output Result
    // ══════════════════════════════════════════════════════════════
    public class ReportOutputResult
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = "report";
        public string ContentType { get; set; } = "application/octet-stream";
        public int RowCount { get; set; }
        public int DurationMs { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // Interface
    // ══════════════════════════════════════════════════════════════
    public interface IReportOutputService
    {
        string Format { get; } // "excel" | "csv"
        Task<ReportOutputResult> GenerateAsync(
            ReportDefinition report,
            List<ReportParam> paramDefs,
            Dictionary<string, string> paramValues,
            MailReportExcelLayoutVM? layout);
    }

    // ══════════════════════════════════════════════════════════════
    // Base — shared helpers
    // ══════════════════════════════════════════════════════════════
    public abstract class ReportOutputBase
    {
        protected readonly string _connStr;
        protected readonly ILogger _logger;

        protected ReportOutputBase(IConfiguration config, ILogger logger)
        {
            _connStr = config.GetConnectionString("DefaultConnection")!;
            _logger = logger;
        }

        // ── Resolve {{placeholder}} ──────────────────────────────
        protected string Resolve(string template, Dictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(template)) return template;

            var now = DateTime.Now;
            var merged = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase)
            {
                ["date"] = now.ToString("yyyy-MM-dd"),
                ["date_label"] = now.ToString("dd MMMM yyyy"),
                ["datetime"] = now.ToString("yyyy-MM-dd HH:mm:ss"),
                ["year"] = now.ToString("yyyy"),
                ["month"] = now.ToString("MM"),
                ["month_name"] = now.ToString("MMMM"),
                ["today"] = now.ToString("yyyy-MM-dd"),
                ["month_start"] = new DateTime(now.Year, now.Month, 1).ToString("yyyy-MM-dd"),
                ["month_end"] = new DateTime(now.Year, now.Month,
                                      DateTime.DaysInMonth(now.Year, now.Month)).ToString("yyyy-MM-dd")
            };

            return Regex.Replace(template, @"\{\{(\w+)\}\}", m =>
                merged.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);
        }

        // ── Inject WHERE ─────────────────────────────────────────
        protected string InjectWhere(string sql, string where)
        {
            sql = sql.TrimEnd(';', ' ');
            var upper = Regex.Replace(sql.ToUpper(), @"\s+", " ");
            bool hasWhere = false;
            int depth = 0;
            for (int i = 0; i < upper.Length - 5; i++)
            {
                if (upper[i] == '(') depth++;
                else if (upper[i] == ')') depth--;
                else if (depth == 0 && i + 5 <= upper.Length)
                {
                    var sub = upper.Substring(i, 5);
                    if (sub == "WHERE")
                    {
                        bool prev = i == 0 || !char.IsLetterOrDigit(upper[i - 1]);
                        bool next = i + 5 >= upper.Length || !char.IsLetterOrDigit(upper[i + 5]);
                        if (prev && next) { hasWhere = true; break; }
                    }
                }
            }
            return hasWhere ? sql + $" AND ({where})" : sql + $" WHERE {where}";
        }

        // ── Safety check ─────────────────────────────────────────
        protected bool IsSafe(string sql)
        {
            var upper = Regex.Replace(sql.ToUpper().Trim(), @"\s+", " ");
            if (!upper.StartsWith("SELECT") && !upper.StartsWith("WITH")) return false;
            var blocked = new[] { " INSERT ", " UPDATE ", " DELETE ", " DROP ",
                                  " TRUNCATE ", " EXEC ", " EXECUTE ", " ALTER ", " CREATE " };
            return !blocked.Any(upper.Contains);
        }

        // ── Run query → DataTable ────────────────────────────────
        protected async Task<DataTable> RunQueryAsync(
            string sqlQuery,
            string? sqlWhere,
            Dictionary<string, string> paramValues,
            string sectionLabel = "")
        {
            var dt = new DataTable();
            if (string.IsNullOrWhiteSpace(sqlQuery)) return dt;

            var sql = sqlQuery.Trim().TrimEnd(';');

            if (!string.IsNullOrWhiteSpace(sqlWhere))
                sql = InjectWhere(sql, Resolve(sqlWhere, paramValues));

            if (!IsSafe(sql))
            {
                _logger.LogWarning("ReportBuilder: unsafe query blocked [{Section}]", sectionLabel);
                return dt;
            }

            sql = Resolve(sql, paramValues);

            try
            {
                await using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };

                foreach (var kv in paramValues)
                {
                    var p = "@" + kv.Key;
                    if (sql.Contains(p)) cmd.Parameters.AddWithValue(p, kv.Value);
                }

                new SqlDataAdapter(cmd).Fill(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReportBuilder: query error [{Section}]", sectionLabel);
                throw new Exception($"Query error on section '{sectionLabel}': {ex.Message}", ex);
            }

            return dt;
        }

        // ── Build filename ────────────────────────────────────────
        protected string BuildFileName(ReportDefinition report, Dictionary<string, string> paramValues, string ext)
        {
            var template = report.filename_template;
            if (string.IsNullOrWhiteSpace(template))
                template = report.report_code + "_{{date}}";

            var name = Resolve(template, paramValues);

            // Sanitize
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            name = name.Replace(' ', '_');

            return name.EndsWith("." + ext, StringComparison.OrdinalIgnoreCase)
                ? name : name + "." + ext;
        }

        // ── Format cell value ─────────────────────────────────────
        protected string FormatCell(object? val, DataColumn col)
        {
            if (val == null || val == DBNull.Value) return "";
            if (col.DataType == typeof(DateTime))
            {
                var dt = (DateTime)val;
                return dt.TimeOfDay == TimeSpan.Zero
                    ? dt.ToString("yyyy-MM-dd")
                    : dt.ToString("yyyy-MM-dd HH:mm");
            }
            if (col.DataType == typeof(TimeSpan))
                return ((TimeSpan)val).ToString(@"hh\:mm\:ss");
            return val.ToString() ?? "";
        }

        protected List<string> GetVisibleCols(string? visibleColumns, DataTable dt)
        {
            if (string.IsNullOrWhiteSpace(visibleColumns))
                return dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            var requested = visibleColumns
                .Split(',')
                .Select(c => c.Trim().Trim('[', ']'))
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            var actual = dt.Columns.Cast<DataColumn>()
                .ToDictionary(c => c.ColumnName.ToLower(), c => c.ColumnName);

            return requested
                .Select(r => actual.TryGetValue(r.ToLower(), out var a) ? a : r)
                .Where(c => dt.Columns.Contains(c))
                .ToList();
        }
    }

    // ══════════════════════════════════════════════════════════════
    // Excel Output Service
    // ══════════════════════════════════════════════════════════════
    public class ExcelOutputService : ReportOutputBase, IReportOutputService
    {
        private readonly IExcelLayoutService _layoutSvc;

        public string Format => "excel";

        public ExcelOutputService(
            IConfiguration config,
            ILogger<ExcelOutputService> logger,
            IExcelLayoutService layoutSvc)
            : base(config, logger)
        {
            _layoutSvc = layoutSvc;
        }

        public async Task<ReportOutputResult> GenerateAsync(
            ReportDefinition report,
            List<ReportParam> paramDefs,
            Dictionary<string, string> paramValues,
            MailReportExcelLayoutVM? layout)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            byte[] data;
            int rowCount = 0;

            if (layout != null && layout.Layout.use_custom_layout == 1)
            {
                // Custom layout via ExcelLayoutService
                data = await _layoutSvc.BuildAsync(report.excel_layout_id!.Value, paramValues, "reportbuilder");
                rowCount = -1; // multi-section, tidak dihitung per baris
            }
            else
            {
                // Flat single sheet — pakai param pertama sebagai query utama
                // Developer wajib set 1 section minimal
                data = BuildFlatExcel(report, paramValues, out rowCount);
            }

            sw.Stop();

            return new ReportOutputResult
            {
                Data = data,
                FileName = BuildFileName(report, paramValues, "xlsx"),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                RowCount = rowCount,
                DurationMs = (int)sw.ElapsedMilliseconds
            };
        }

        // ── Flat Excel — fallback kalau tidak ada custom layout ──
        private byte[] BuildFlatExcel(
            ReportDefinition report,
            Dictionary<string, string> paramValues,
            out int rowCount)
        {
            rowCount = 0;
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Data");

            // Title
            var titleCell = ws.Cell(1, 1);
            titleCell.Value = Resolve(report.report_name, paramValues);
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 14;

            // Subtitle (params)
            var subtitle = string.Join("  |  ", paramValues
                .Where(kv => !kv.Key.StartsWith("_"))
                .Select(kv => $"{kv.Key}: {kv.Value}"));
            ws.Cell(2, 1).Value = subtitle;
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Cell(2, 1).Style.Font.FontSize = 9;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

            int row = 4;

            // Note: developer harus set excel_layout_id untuk custom layout
            // Kalau tidak ada, tampilkan pesan
            ws.Cell(row, 1).Value = "No layout configured. Please set Excel Layout in Report Builder.";
            ws.Cell(row, 1).Style.Font.Italic = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.Orange;

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }

    // ══════════════════════════════════════════════════════════════
    // CSV Output Service
    // ══════════════════════════════════════════════════════════════
    public class CsvOutputService : ReportOutputBase, IReportOutputService
    {
        public string Format => "csv";

        public CsvOutputService(IConfiguration config, ILogger<CsvOutputService> logger)
            : base(config, logger) { }

        public async Task<ReportOutputResult> GenerateAsync(
            ReportDefinition report,
            List<ReportParam> paramDefs,
            Dictionary<string, string> paramValues,
            MailReportExcelLayoutVM? layout)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Untuk CSV: ambil semua section dari layout, gabung jadi 1 flat CSV
            // Kalau tidak ada layout, return empty
            var sb = new StringBuilder();
            int rowCount = 0;

            if (layout != null && layout.Layout.use_custom_layout == 1)
            {
                bool firstSheet = true;
                foreach (var sheetVM in layout.Sheets.OrderBy(s => s.Sheet.sort_order))
                {
                    if (!firstSheet) { sb.AppendLine(); sb.AppendLine(); }
                    firstSheet = false;

                    sb.AppendLine($"# Sheet: {sheetVM.Sheet.sheet_name}");

                    foreach (var sec in sheetVM.Sections.OrderBy(s => s.sort_order))
                    {
                        if (!string.IsNullOrWhiteSpace(sec.section_title))
                            sb.AppendLine($"# {Resolve(sec.section_title, paramValues)}");

                        var dt = await RunQueryAsync(sec.sql_query!, sec.sql_where, paramValues, sec.section_label);
                        var cols = GetVisibleCols(sec.visible_columns, dt);

                        // Header
                        sb.AppendLine(string.Join(",", cols.Select(CsvEscape)));

                        // Rows
                        foreach (DataRow dr in dt.Rows)
                        {
                            sb.AppendLine(string.Join(",",
                                cols.Select(c => CsvEscape(FormatCell(
                                    dt.Columns.Contains(c) ? dr[c] : DBNull.Value,
                                    dt.Columns.Contains(c) ? dt.Columns[c]! : new DataColumn())))));
                            rowCount++;
                        }

                        sb.AppendLine();
                    }
                }
            }
            else
            {
                sb.AppendLine("# No layout configured.");
            }

            sw.Stop();

            var bytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
                .ToArray();

            return new ReportOutputResult
            {
                Data = bytes,
                FileName = BuildFileName(report, paramValues, "csv"),
                ContentType = "text/csv; charset=utf-8",
                RowCount = rowCount,
                DurationMs = (int)sw.ElapsedMilliseconds
            };
        }

        private string CsvEscape(string? val)
        {
            if (val == null) return "";
            if (val.Contains(',') || val.Contains('"') || val.Contains('\n'))
                return "\"" + val.Replace("\"", "\"\"") + "\"";
            return val;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // Report Runner Service — orchestrator utama
    // ══════════════════════════════════════════════════════════════
    public interface IReportRunnerService
    {
        Task<ReportOutputResult> RunAsync(ReportGenerateRequest request, string username);
        Task<List<ReportDefinition>> GetAccessibleReportsAsync(string username, string? roleName);
        Task<List<ReportParamOptionItem>> GetParamOptionsAsync(int reportId, string paramKey, Dictionary<string, string> currentValues);
    }

    public class ReportRunnerService : IReportRunnerService
    {
        private readonly AppDbContext _db;
        private readonly IEnumerable<IReportOutputService> _outputServices;
        private readonly IExcelLayoutService _layoutSvc;
        private readonly ILogger<ReportRunnerService> _logger;
        private readonly string _connStr;

        public ReportRunnerService(
            AppDbContext db,
            IEnumerable<IReportOutputService> outputServices,
            IExcelLayoutService layoutSvc,
            ILogger<ReportRunnerService> logger,
            IConfiguration config)
        {
            _db = db;
            _outputServices = outputServices;
            _layoutSvc = layoutSvc;
            _logger = logger;
            _connStr = config.GetConnectionString("DefaultConnection")!;
        }

        // ── RunAsync ─────────────────────────────────────────────
        public async Task<ReportOutputResult> RunAsync(ReportGenerateRequest request, string username)
        {
            var report = await _db.ReportDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ID == request.ReportId && r.is_active == 1)
                ?? throw new Exception("Report not found or inactive.");

            // Cek permission
            var hasAccess = await _db.ReportPermissions
                .AnyAsync(p => p.report_id == report.ID &&
                               (p.role_name == "ALL" || p.role_name == username));
            // Note: untuk production ganti dengan role-based check dari session

            var paramDefs = await _db.ReportParams
                .AsNoTracking()
                .Where(p => p.report_id == report.ID)
                .OrderBy(p => p.sort_order)
                .ToListAsync();

            // Merge default values untuk hidden params
            foreach (var def in paramDefs.Where(p => p.is_hidden == 1))
            {
                if (!request.Params.ContainsKey(def.param_key) && !string.IsNullOrEmpty(def.default_value))
                    request.Params[def.param_key] = ResolveDefaultValue(def.default_value);
            }

            // Load layout kalau ada
            MailReportExcelLayoutVM? layout = null;
            if (report.excel_layout_id.HasValue)
                layout = await _layoutSvc.LoadForFormAsync(report.excel_layout_id.Value);

            // Pilih output service
            var outputSvc = _outputServices
                .FirstOrDefault(s => s.Format == request.OutputFormat.ToLower())
                ?? throw new Exception($"Output format '{request.OutputFormat}' not supported.");

            ReportOutputResult result;
            var logEntry = new ReportRunLog
            {
                report_id = report.ID,
                run_by = username,
                params_json = System.Text.Json.JsonSerializer.Serialize(request.Params),
                output_format = request.OutputFormat,
                run_at = DateTime.Now
            };

            try
            {
                result = await outputSvc.GenerateAsync(report, paramDefs, request.Params, layout);

                logEntry.status = "SUCCESS";
                logEntry.row_count = result.RowCount;
                logEntry.duration_ms = result.DurationMs;
            }
            catch (Exception ex)
            {
                logEntry.status = "FAILED";
                logEntry.error_message = ex.Message;
                _logger.LogError(ex, "ReportRunner: failed report {Code}", report.report_code);

                _db.ReportRunLogs.Add(logEntry);
                await _db.SaveChangesAsync();
                throw;
            }

            _db.ReportRunLogs.Add(logEntry);
            await _db.SaveChangesAsync();

            return result;
        }

        // ── GetAccessibleReports ─────────────────────────────────
        public async Task<List<ReportDefinition>> GetAccessibleReportsAsync(string username, string? roleName)
        {
            // Ambil report yang punya permission ALL, atau match username/roleName
            var accessibleIds = await _db.ReportPermissions
                .Where(p => p.role_name == "ALL"
                         || p.role_name == username
                         || (roleName != null && p.role_name == roleName))
                .Select(p => p.report_id)
                .Distinct()
                .ToListAsync();

            return await _db.ReportDefinitions
                .AsNoTracking()
                .Where(r => r.is_active == 1 && accessibleIds.Contains(r.ID))
                .OrderBy(r => r.category)
                .ThenBy(r => r.report_name)
                .ToListAsync();
        }

        // ── GetParamOptions — untuk dropdown dinamis ─────────────
        public async Task<List<ReportParamOptionItem>> GetParamOptionsAsync(
            int reportId,
            string paramKey,
            Dictionary<string, string> currentValues)
        {
            var param = await _db.ReportParams
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.report_id == reportId && p.param_key == paramKey)
                ?? throw new Exception("Param not found.");

            var result = new List<ReportParamOptionItem>();

            if (param.options_source == "query" && !string.IsNullOrWhiteSpace(param.param_options))
            {
                var sql = param.param_options.Trim().TrimEnd(';');
                sql = ResolveInline(sql, currentValues);

                await using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 15 };
                await using var reader = await cmd.ExecuteReaderAsync();

                var valCol = param.options_value_col ?? reader.GetName(0);
                var textCol = param.options_text_col ?? (reader.FieldCount > 1 ? reader.GetName(1) : valCol);

                while (await reader.ReadAsync())
                {
                    result.Add(new ReportParamOptionItem
                    {
                        Value = reader[valCol]?.ToString() ?? "",
                        Text = reader[textCol]?.ToString() ?? ""
                    });
                }
            }
            else if (!string.IsNullOrWhiteSpace(param.param_options))
            {
                // Static options: "ACTIVE,INACTIVE" atau "1=Active,2=Inactive"
                foreach (var opt in param.param_options.Split(','))
                {
                    var parts = opt.Trim().Split('=');
                    result.Add(new ReportParamOptionItem
                    {
                        Value = parts[0].Trim(),
                        Text = parts.Length > 1 ? parts[1].Trim() : parts[0].Trim()
                    });
                }
            }

            return result;
        }

        private string ResolveDefaultValue(string val)
        {
            var now = DateTime.Now;
            return val
                .Replace("{{today}}", now.ToString("yyyy-MM-dd"))
                .Replace("{{month_start}}", new DateTime(now.Year, now.Month, 1).ToString("yyyy-MM-dd"))
                .Replace("{{month_end}}", new DateTime(now.Year, now.Month,
                    DateTime.DaysInMonth(now.Year, now.Month)).ToString("yyyy-MM-dd"))
                .Replace("{{year}}", now.ToString("yyyy"))
                .Replace("{{month}}", now.ToString("MM"));
        }

        private string ResolveInline(string sql, Dictionary<string, string> values)
        {
            return Regex.Replace(sql, @"\{\{(\w+)\}\}", m =>
                values.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);
        }
    }
}