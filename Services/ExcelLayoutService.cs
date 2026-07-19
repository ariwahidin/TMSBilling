
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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
    public interface IExcelLayoutService
    {
        // owner_type: "mailreport" | "reportbuilder"
        Task<bool> HasCustomLayoutAsync(int reportId, string ownerType = "mailreport");
        Task<byte[]> BuildAsync(int reportId, Dictionary<string, string> eventParams, string ownerType = "mailreport");
        Task<MailReportExcelLayoutVM> LoadForFormAsync(int reportId, string ownerType = "mailreport");
        Task SaveFromFormAsync(int reportId, MailReportExcelLayoutVM vm, string ownerType = "mailreport");
    }

    public class ExcelLayoutService : IExcelLayoutService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ExcelLayoutService> _logger;
        private readonly string _connStr;
        private readonly IWebHostEnvironment _env;

        public ExcelLayoutService(
            AppDbContext db,
            ILogger<ExcelLayoutService> logger,
            IConfiguration config,
            IWebHostEnvironment env)
        {
            _db = db;
            _logger = logger;
            _connStr = config.GetConnectionString("DefaultConnection")!;
            _env = env;
        }

        // ──────────────────────────────────────────────
        // HasCustomLayoutAsync
        // ──────────────────────────────────────────────
        public async Task<bool> HasCustomLayoutAsync(int reportId, string ownerType = "mailreport")
        {
            var layout = await _db.MailReportExcelLayouts
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.report_id == reportId && l.owner_type == ownerType);
            return layout != null && layout.use_custom_layout == 1;
        }

        // ──────────────────────────────────────────────
        // LoadForFormAsync
        // ──────────────────────────────────────────────
        //public async Task<MailReportExcelLayoutVM> LoadForFormAsync(int reportId, string ownerType = "mailreport")
        //{
        //    //var layout = await _db.MailReportExcelLayouts
        //    //    .AsNoTracking()
        //    //    .FirstOrDefaultAsync(l => l.report_id == reportId && l.owner_type == ownerType);

        //    // DEBUG — hapus setelah selesai
        //    var query = _db.MailReportExcelLayouts
        //        .Where(l => l.report_id == reportId && l.owner_type == ownerType);

        //    Console.WriteLine($"[DEBUG LoadForFormAsync] reportId={reportId}, ownerType={ownerType}");
        //    Console.WriteLine($"[DEBUG SQL] {query.ToQueryString()}");

        //    var layout = await query.AsNoTracking().FirstOrDefaultAsync();
        //    Console.WriteLine($"[DEBUG] reportId={reportId} ownerType={ownerType} layoutID={layout?.ID ?? -1}");

        //    Console.WriteLine($"[DEBUG] layout found: {layout?.ID.ToString() ?? "NULL"}");
        //    // ─────────────────────────────────────────

        //    if (layout == null)
        //        return new MailReportExcelLayoutVM
        //        {
        //            Layout = new MailReportExcelLayout { report_id = reportId, owner_type = ownerType },
        //            Sheets = new List<MailReportExcelSheetVM>()
        //        };

        //    var sheets = await _db.MailReportExcelSheets
        //        .AsNoTracking()
        //        .Where(s => s.layout_id == layout.ID)
        //        .OrderBy(s => s.sort_order)
        //        .ToListAsync();

        //    Console.WriteLine($"[DEBUG] sheets={sheets.Count}");

        //    var sheetIds = sheets.Select(s => s.ID).ToList();
        //    var allSections = await _db.MailReportExcelSections
        //        .AsNoTracking()
        //        .Where(s => sheetIds.Contains(s.sheet_id))
        //        .OrderBy(s => s.sort_order)
        //        .ToListAsync();

        //    return new MailReportExcelLayoutVM
        //    {
        //        Layout = layout,
        //        Sheets = sheets.Select(s => new MailReportExcelSheetVM
        //        {
        //            Sheet = s,
        //            Sections = allSections.Where(sec => sec.sheet_id == s.ID).ToList()
        //        }).ToList()
        //    };
        //}

        // ──────────────────────────────────────────────
        // SaveFromFormAsync
        // ──────────────────────────────────────────────
        //public async Task SaveFromFormAsync(int reportId, MailReportExcelLayoutVM vm, string ownerType = "mailreport")
        //{
        //    // Upsert layout header — filter by owner_type supaya tidak nabrak
        //    var layout = await _db.MailReportExcelLayouts
        //        .FirstOrDefaultAsync(l => l.report_id == reportId && l.owner_type == ownerType);

        //    if (layout == null)
        //    {
        //        layout = new MailReportExcelLayout { report_id = reportId, owner_type = ownerType };
        //        _db.MailReportExcelLayouts.Add(layout);
        //    }

        //    layout.use_custom_layout = vm.Layout.use_custom_layout;
        //    layout.report_title = vm.Layout.report_title;
        //    layout.report_subtitle = vm.Layout.report_subtitle;
        //    layout.title_bg_color = vm.Layout.title_bg_color ?? "FFFFFF";
        //    layout.title_font_color = vm.Layout.title_font_color ?? "000000";
        //    layout.title_font_size = vm.Layout.title_font_size > 0 ? vm.Layout.title_font_size : 14;

        //    await _db.SaveChangesAsync();

        //    // Update excel_layout_id di ReportDefinition supaya BuildAsync bisa load layout
        //    if (ownerType == "reportbuilder")
        //    {
        //        var report = await _db.ReportDefinitions.FindAsync(reportId);
        //        if (report != null && report.excel_layout_id != layout.ID)
        //        {
        //            report.excel_layout_id = layout.ID;
        //            await _db.SaveChangesAsync();
        //        }
        //    }

        //    // Replace sheets + sections
        //    var oldSheets = _db.MailReportExcelSheets.Where(s => s.layout_id == layout.ID);
        //    _db.MailReportExcelSheets.RemoveRange(oldSheets);
        //    await _db.SaveChangesAsync();

        //    foreach (var (sheetVM, si) in vm.Sheets.Select((s, i) => (s, i)))
        //    {
        //        var sheet = new MailReportExcelSheet
        //        {
        //            layout_id = layout.ID,
        //            sheet_name = sheetVM.Sheet.sheet_name,
        //            sort_order = si
        //        };
        //        _db.MailReportExcelSheets.Add(sheet);
        //        await _db.SaveChangesAsync();

        //        foreach (var (sec, secIdx) in sheetVM.Sections.Select((s, i) => (s, i)))
        //        {
        //            sec.ID = 0;
        //            sec.sheet_id = sheet.ID;
        //            sec.sort_order = secIdx;
        //            _db.MailReportExcelSections.Add(sec);
        //        }
        //    }

        //    await _db.SaveChangesAsync();
        //}

        // ──────────────────────────────────────────────
        // BuildAsync
        // ──────────────────────────────────────────────
        public async Task<byte[]> BuildAsync(int reportId, Dictionary<string, string> eventParams, string ownerType = "mailreport")
        {
            var vm = await LoadForFormAsync(reportId, ownerType);

            // DEBUG
            Console.WriteLine($"[BuildAsync] reportId={reportId}, ownerType={ownerType}");
            Console.WriteLine($"[BuildAsync] layout.ID={vm.Layout.ID}, use_custom={vm.Layout.use_custom_layout}");
            Console.WriteLine($"[BuildAsync] sheets count={vm.Sheets.Count}");


            var resolvedTitle = Resolve(vm.Layout.report_title ?? "", eventParams);
            var resolvedSubtitle = Resolve(vm.Layout.report_subtitle ?? "", eventParams);

            using var wb = new XLWorkbook();

            foreach (var sheetVM in vm.Sheets.OrderBy(s => s.Sheet.sort_order))
            {
                var sheetName = SanitizeSheetName(Resolve(sheetVM.Sheet.sheet_name, eventParams));
                var ws = wb.Worksheets.Add(sheetName);
                await BuildSheetAsync(ws, sheetVM, vm.Layout, resolvedTitle, resolvedSubtitle, eventParams);
            }

            if (!wb.Worksheets.Any())
                wb.Worksheets.Add("Sheet1");

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        // ──────────────────────────────────────────────
        // BuildSheetAsync
        // ──────────────────────────────────────────────
        private async Task BuildSheetAsync(
            IXLWorksheet ws,
            MailReportExcelSheetVM sheetVM,
            MailReportExcelLayout layout,
            string resolvedTitle,
            string resolvedSubtitle,
            Dictionary<string, string> eventParams)
        {
            int currentRow = 1;
            int totalCols = sheetVM.Sections
                .SelectMany(s => (s.visible_columns ?? "").Split(',').Where(c => !string.IsNullOrWhiteSpace(c)))
                .Count();
            totalCols = Math.Max(totalCols, 5); // minimal 5 kolom untuk merge

            // ── Report Title ──────────────────────────────
            if (!string.IsNullOrWhiteSpace(resolvedTitle))
            {
                var titleRange = ws.Range(currentRow, 1, currentRow, totalCols);
                titleRange.Merge();
                titleRange.FirstCell().Value = resolvedTitle;
                titleRange.Style.Font.Bold = true;
                titleRange.Style.Font.FontSize = layout.title_font_size;
                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                SetFontColor(titleRange.Style.Font, layout.title_font_color ?? "000000");
                if (!string.IsNullOrWhiteSpace(layout.title_bg_color) && layout.title_bg_color != "FFFFFF")
                    SetFill(titleRange.Style.Fill, layout.title_bg_color);
                currentRow++;
            }

            // ── Sub Title ─────────────────────────────────
            if (!string.IsNullOrWhiteSpace(resolvedSubtitle))
            {
                var subRange = ws.Range(currentRow, 1, currentRow, totalCols);
                subRange.Merge();
                subRange.FirstCell().Value = resolvedSubtitle;
                subRange.Style.Font.Italic = true;
                subRange.Style.Font.FontSize = Math.Max(layout.title_font_size - 2, 9);
                subRange.Style.Font.FontColor = XLColor.FromHtml("#555555");
                subRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                currentRow++;
            }

            if (!string.IsNullOrWhiteSpace(resolvedTitle) || !string.IsNullOrWhiteSpace(resolvedSubtitle))
                currentRow++; // 1 baris kosong setelah header

            var groups = GroupSections(sheetVM.Sections.OrderBy(s => s.sort_order).ToList());

            int? freezeRow = null; // baris yang akan di-freeze

            foreach (var group in groups)
            {
                int bottomGap = group[0].bottom_gap > 0 ? group[0].bottom_gap : 2;

                if (group.Count == 1 || group[0].layout != "side_by_side")
                {
                    foreach (var sec in group)
                    {
                        var dt = await RunQueryAsync(sec, eventParams);
                        // Catat posisi header row pertama untuk freeze
                        if (freezeRow == null && sec.display_mode != "KEY_VALUE" && dt.Rows.Count > 0)
                        {
                            int headerRow = currentRow;
                            if (!string.IsNullOrWhiteSpace(sec.section_title)) headerRow++;
                            freezeRow = headerRow + 1; // freeze setelah header
                        }
                        currentRow = RenderSection(ws, sec, dt, currentRow, 1, eventParams);
                        currentRow += sec.bottom_gap > 0 ? sec.bottom_gap : 2;
                    }
                }
                else
                {
                    currentRow = await RenderSideBySideAsync(ws, group, currentRow, eventParams);
                    currentRow += bottomGap;
                }
            }

            // ── Auto column width dengan max cap ──────────
            foreach (var col in ws.ColumnsUsed())
            {
                col.AdjustToContents();
                if (col.Width > 50) col.Width = 50; // max 50 chars width
            }

            // ── Freeze panes — beku di bawah header row ──
            if (freezeRow.HasValue && freezeRow.Value > 1)
                ws.SheetView.FreezeRows(freezeRow.Value - 1);

            // ── Auto filter pada header row pertama ───────
            if (freezeRow.HasValue)
            {
                var headerRowNum = freezeRow.Value - 1;
                var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 1;
                if (lastCol > 1)
                    ws.Range(headerRowNum, 1, headerRowNum, lastCol).SetAutoFilter();
            }
        }

        // ──────────────────────────────────────────────
        // GroupSections
        // ──────────────────────────────────────────────
        private List<List<MailReportExcelSection>> GroupSections(List<MailReportExcelSection> sections)
        {
            var result = new List<List<MailReportExcelSection>>();
            var visited = new HashSet<int>();

            foreach (var sec in sections)
            {
                if (visited.Contains(sec.ID)) continue;

                if (sec.layout == "side_by_side" && sec.group_id > 0)
                {
                    var group = sections
                        .Where(s => s.layout == "side_by_side" && s.group_id == sec.group_id)
                        .OrderBy(s => s.sort_order)
                        .ToList();
                    result.Add(group);
                    foreach (var g in group) visited.Add(g.ID);
                }
                else
                {
                    result.Add(new List<MailReportExcelSection> { sec });
                    visited.Add(sec.ID);
                }
            }
            return result;
        }

        // ──────────────────────────────────────────────
        // RenderSideBySideAsync
        // ──────────────────────────────────────────────
        private async Task<int> RenderSideBySideAsync(
            IXLWorksheet ws,
            List<MailReportExcelSection> sections,
            int startRow,
            Dictionary<string, string> eventParams)
        {
            int currentCol = 1;
            int maxRow = startRow;

            foreach (var sec in sections)
            {
                var dt = await RunQueryAsync(sec, eventParams);
                var endRow = RenderSection(ws, sec, dt, startRow, currentCol, eventParams);
                maxRow = Math.Max(maxRow, endRow);
                var colCount = Math.Max(GetVisibleColumns(sec, dt).Count, 1);
                currentCol += colCount + (sec.side_gap > 0 ? sec.side_gap : 1);
            }

            return maxRow;
        }

        // ──────────────────────────────────────────────
        // RenderSection
        // ──────────────────────────────────────────────
        private int RenderSection(
            IXLWorksheet ws,
            MailReportExcelSection sec,
            DataTable dt,
            int startRow,
            int startCol,
            Dictionary<string, string> eventParams)
        {
            int row = startRow;
            var visibleCols = GetVisibleColumns(sec, dt);
            int colCount = Math.Max(visibleCols.Count, 1);

            // Section title
            if (!string.IsNullOrWhiteSpace(sec.section_title))
            {
                //var titleText = Resolve(sec.section_title, eventParams);
                var titleText = ResolveWithDataTable(sec.section_title, dt, eventParams);
                var titleRange = ws.Range(row, startCol, row, startCol + colCount - 1);
                titleRange.Merge();
                titleRange.FirstCell().Value = titleText;
                titleRange.Style.Font.Bold = true;
                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                SetFill(titleRange.Style.Fill, sec.title_bg_color ?? "FFD700");
                SetFontColor(titleRange.Style.Font, sec.title_font_color ?? "000000");
                titleRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                row++;
            }

            if (dt.Rows.Count == 0)
            {
                ws.Cell(row, startCol).Value = "Tidak ada data.";
                ws.Cell(row, startCol).Style.Font.Italic = true;
                ws.Cell(row, startCol).Style.Font.FontColor = XLColor.Gray;
                return row + 1;
            }

            // KEY_VALUE mode
            if (sec.display_mode == "KEY_VALUE")
            {
                foreach (DataColumn col in dt.Columns)
                {
                    if (!visibleCols.Contains(col.ColumnName)) continue;
                    var keyCell = ws.Cell(row, startCol);
                    keyCell.Value = col.ColumnName;
                    keyCell.Style.Font.Bold = true;
                    keyCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F7F9FC");
                    keyCell.Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                    var valCell = ws.Cell(row, startCol + 1);
                    valCell.Value = FormatCell(dt.Rows[0][col.ColumnName], col);
                    valCell.Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                    row++;
                }
                return row;
            }

            // TABLE mode
            var numericTypes = new[]
            {
                typeof(int), typeof(long), typeof(short), typeof(byte),
                typeof(decimal), typeof(double), typeof(float)
            };
            var numericCols = visibleCols
                .Where(c => dt.Columns.Contains(c) && numericTypes.Contains(dt.Columns[c]!.DataType))
                .ToHashSet();

            // Header
            for (int c = 0; c < visibleCols.Count; c++)
            {
                var hCell = ws.Cell(row, startCol + c);
                hCell.Value = visibleCols[c];
                hCell.Style.Font.Bold = true;
                SetFill(hCell.Style.Fill, sec.header_bg_color ?? "FFD700");
                SetFontColor(hCell.Style.Font, sec.header_font_color ?? "000000");
                hCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                hCell.Style.Alignment.WrapText = false;
            }
            row++;

            // Data rows
            var totals = new Dictionary<string, decimal>();
            int dataRowStart = row;

            foreach (DataRow dataRow in dt.Rows)
            {
                for (int c = 0; c < visibleCols.Count; c++)
                {
                    var colName = visibleCols[c];
                    var cell = ws.Cell(row, startCol + c);
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Hair;

                    if (!dt.Columns.Contains(colName)) { cell.Value = ""; continue; }

                    var rawVal = dataRow[colName];
                    var colDef = dt.Columns[colName]!;

                    if (numericCols.Contains(colName) && rawVal != DBNull.Value)
                    {
                        cell.Value = Convert.ToDouble(rawVal);
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        if (sec.show_grand_total == 1)
                        {
                            if (!totals.ContainsKey(colName)) totals[colName] = 0;
                            totals[colName] += Convert.ToDecimal(rawVal);
                        }
                    }
                    else
                    {
                        cell.Value = FormatCell(rawVal, colDef);
                    }
                }

                if ((row - dataRowStart) % 2 == 1)
                {
                    ws.Range(row, startCol, row, startCol + visibleCols.Count - 1)
                      .Style.Fill.BackgroundColor = XLColor.FromHtml("#F7F9FC");
                }
                row++;
            }

            // Grand Total
            if (sec.show_grand_total == 1)
            {
                var label = sec.grand_total_label ?? "TOTAL";
                bool labelPlaced = false;

                for (int c = 0; c < visibleCols.Count; c++)
                {
                    var colName = visibleCols[c];
                    var cell = ws.Cell(row, startCol + c);
                    SetFill(cell.Style.Fill, sec.total_bg_color ?? "FFD700");
                    cell.Style.Font.Bold = true;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    if (!labelPlaced && !numericCols.Contains(colName))
                    {
                        cell.Value = label;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        labelPlaced = true;
                    }
                    else if (numericCols.Contains(colName))
                    {
                        if (!labelPlaced) { cell.Value = label; labelPlaced = true; }
                        else if (totals.TryGetValue(colName, out var t))
                        {
                            cell.Value = (double)t;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        }
                    }
                }
                row++;
            }

            return row;
        }

        // ──────────────────────────────────────────────
        // RunQueryAsync
        // ──────────────────────────────────────────────
        private async Task<DataTable> RunQueryAsync(
            MailReportExcelSection sec,
            Dictionary<string, string> eventParams)
        {
            var dt = new DataTable();
            if (string.IsNullOrWhiteSpace(sec.sql_query)) return dt;

            var sql = sec.sql_query.Trim().TrimEnd(';');

            if (!string.IsNullOrWhiteSpace(sec.sql_where))
                sql = InjectWhere(sql, Resolve(sec.sql_where, eventParams));

            if (!IsSafe(sql))
            {
                _logger.LogWarning("ExcelLayout: query tidak aman diblokir [{Label}]", sec.section_label);
                return dt;
            }

            sql = Resolve(sql, eventParams);

            _logger.LogInformation("ExcelLayout SQL [{Label}]:\n{Sql}", sec.section_label, sql);

            try
            {
                await using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
                foreach (var kv in eventParams)
                {
                    var p = "@" + kv.Key;
                    if (sql.Contains(p)) cmd.Parameters.AddWithValue(p, kv.Value);
                }
                new SqlDataAdapter(cmd).Fill(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExcelLayout: query error [{Label}]", sec.section_label);
                dt.Columns.Add("Error");
                dt.Rows.Add(ex.Message);
            }

            return dt;
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────
        private List<string> GetVisibleColumns(MailReportExcelSection sec, DataTable dt)
        {
            if (string.IsNullOrWhiteSpace(sec.visible_columns))
                return dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            var requested = sec.visible_columns
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

        private string FormatCell(object? val, DataColumn col)
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

        private string Resolve(string template, Dictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(template)) return template;
            var merged = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase)
            {
                ["date"] = DateTime.Now.ToString("yyyy-MM-dd"),
                ["date_label"] = DateTime.Now.ToString("dd MMMM yyyy"),
                ["datetime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ["year"] = DateTime.Now.ToString("yyyy"),
                ["month"] = DateTime.Now.ToString("MM"),
                ["month_name"] = DateTime.Now.ToString("MMMM"),
                ["today"] = DateTime.Now.ToString("yyyy-MM-dd"),
                ["month_start"] = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("yyyy-MM-dd"),
                ["month_end"] = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                                      DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)).ToString("yyyy-MM-dd")
            };
            return Regex.Replace(template, @"\{\{(\w+)\}\}", m =>
                merged.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);
        }

        private string ResolveWithDataTable(
        string template,
        DataTable dt,
        Dictionary<string, string>? values = null)
        {
            if (string.IsNullOrEmpty(template)) return template;

            // base values (optional dari luar)
            var merged = new Dictionary<string, string>(
                values ?? new Dictionary<string, string>(),
                StringComparer.OrdinalIgnoreCase
            );

            // default system values
            merged["date"] = DateTime.Now.ToString("yyyy-MM-dd");
            merged["date_label"] = DateTime.Now.ToString("dd MMMM yyyy");
            merged["datetime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            merged["year"] = DateTime.Now.ToString("yyyy");
            merged["month"] = DateTime.Now.ToString("MM");
            merged["month_name"] = DateTime.Now.ToString("MMMM");
            merged["today"] = DateTime.Now.ToString("yyyy-MM-dd");
            merged["month_start"] = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("yyyy-MM-dd");
            merged["month_end"] = new DateTime(
                DateTime.Now.Year,
                DateTime.Now.Month,
                DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)
            ).ToString("yyyy-MM-dd");

            // ambil dari DataTable row pertama
            if (dt != null && dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                foreach (DataColumn col in dt.Columns)
                {
                    var val = row[col];
                    merged[col.ColumnName] = val == DBNull.Value ? "" : val.ToString()!;
                }
            }

            // replace {{key}}
            return Regex.Replace(template, @"\{\{(\w+)\}\}", m =>
                merged.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value
            );
        }

        private string InjectWhere(string sql, string where)
        {
            sql = sql.TrimEnd(';', ' ');
            var upper = Regex.Replace(sql.ToUpper(), @"\s+", " ");

            bool hasWhere = false;
            int orderByPos = -1;
            int depth = 0;

            for (int i = 0; i < upper.Length; i++)
            {
                if (upper[i] == '(') { depth++; continue; }
                if (upper[i] == ')') { depth--; continue; }
                if (depth != 0) continue;

                if (!hasWhere && i + 5 <= upper.Length && upper.Substring(i, 5) == "WHERE")
                {
                    bool prev = i == 0 || !char.IsLetterOrDigit(upper[i - 1]);
                    bool next = i + 5 >= upper.Length || !char.IsLetterOrDigit(upper[i + 5]);
                    if (prev && next) hasWhere = true;
                }

                if (orderByPos == -1 && i + 8 <= upper.Length && upper.Substring(i, 8) == "ORDER BY")
                {
                    bool prev = i == 0 || !char.IsLetterOrDigit(upper[i - 1]);
                    bool next = i + 8 >= upper.Length || !char.IsLetterOrDigit(upper[i + 8]);
                    if (prev && next) orderByPos = i;
                }
            }

            if (hasWhere)
            {
                if (orderByPos > 0)
                    return sql.Substring(0, orderByPos).TrimEnd() + $" AND ({where}) " + sql.Substring(orderByPos);
                return sql + $" AND ({where})";
            }
            else
            {
                if (orderByPos > 0)
                    return sql.Substring(0, orderByPos).TrimEnd() + $" WHERE {where} " + sql.Substring(orderByPos);
                return sql + $" WHERE {where}";
            }
        }

        private bool IsSafe(string sql)
        {
            var upper = Regex.Replace(sql.ToUpper().Trim(), @"\s+", " ");
            if (!upper.StartsWith("SELECT") && !upper.StartsWith("WITH")) return false;
            var blocked = new[] { " INSERT ", " UPDATE ", " DELETE ", " DROP ",
                                  " TRUNCATE ", " EXEC ", " EXECUTE ", " ALTER ", " CREATE " };
            return !blocked.Any(upper.Contains);
        }

        private void SetFill(IXLFill fill, string? hex)
        {
            try { if (!string.IsNullOrWhiteSpace(hex)) fill.BackgroundColor = XLColor.FromHtml("#" + hex.TrimStart('#')); }
            catch { /* ignore */ }
        }

        private void SetFontColor(IXLFont font, string? hex)
        {
            try { if (!string.IsNullOrWhiteSpace(hex)) font.FontColor = XLColor.FromHtml("#" + hex.TrimStart('#')); }
            catch { /* ignore */ }
        }

        private string SanitizeSheetName(string name)
        {
            foreach (var c in new[] { ':', '\\', '/', '?', '*', '[', ']' }) name = name.Replace(c, '_');
            if (name.Length > 31) name = name[..31];
            return string.IsNullOrWhiteSpace(name) ? "Sheet" : name;
        }


        // ──────────────────────────────────────────────
        // LoadForFormAsync
        // ──────────────────────────────────────────────
        public async Task<MailReportExcelLayoutVM> LoadForFormAsync(int reportId, string ownerType = "mailreport")
        {
            var layout = await _db.MailReportExcelLayouts
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.report_id == reportId && l.owner_type == ownerType);

            if (layout == null)
                return new MailReportExcelLayoutVM
                {
                    Layout = new MailReportExcelLayout { report_id = reportId, owner_type = ownerType },
                    Sheets = new List<MailReportExcelSheetVM>(),
                    Signatures = new List<MailReportSignature>()
                };

            var sheets = await _db.MailReportExcelSheets
                .AsNoTracking()
                .Where(s => s.layout_id == layout.ID)
                .OrderBy(s => s.sort_order)
                .ToListAsync();

            var sheetIds = sheets.Select(s => s.ID).ToList();
            var allSections = await _db.MailReportExcelSections
                .AsNoTracking()
                .Where(s => sheetIds.Contains(s.sheet_id))
                .OrderBy(s => s.sort_order)
                .ToListAsync();

            var signatures = await _db.MailReportSignatures
                .AsNoTracking()
                .Where(s => s.layout_id == layout.ID)
                .OrderBy(s => s.sort_order)
                .ToListAsync();

            return new MailReportExcelLayoutVM
            {
                Layout = layout,
                Sheets = sheets.Select(s => new MailReportExcelSheetVM
                {
                    Sheet = s,
                    Sections = allSections.Where(sec => sec.sheet_id == s.ID).ToList()
                }).ToList(),
                Signatures = signatures
            };
        }

        // ──────────────────────────────────────────────
        // SaveFromFormAsync
        // ──────────────────────────────────────────────
        public async Task SaveFromFormAsync(int reportId, MailReportExcelLayoutVM vm, string ownerType = "mailreport")
        {
            var layout = await _db.MailReportExcelLayouts
                .FirstOrDefaultAsync(l => l.report_id == reportId && l.owner_type == ownerType);

            if (layout == null)
            {
                layout = new MailReportExcelLayout { report_id = reportId, owner_type = ownerType };
                _db.MailReportExcelLayouts.Add(layout);
            }

            layout.use_custom_layout = vm.Layout.use_custom_layout;
            layout.report_title = vm.Layout.report_title;
            layout.report_subtitle = vm.Layout.report_subtitle;
            layout.title_bg_color = vm.Layout.title_bg_color ?? "FFFFFF";
            layout.title_font_color = vm.Layout.title_font_color ?? "000000";
            layout.title_font_size = vm.Layout.title_font_size > 0 ? vm.Layout.title_font_size : 14;
            //layout.logo_path = vm.Layout.logo_path;
            var oldLogoPath = layout.logo_path;
            var newLogoPath = vm.Layout.logo_path;


            if (!string.IsNullOrWhiteSpace(oldLogoPath) && oldLogoPath != newLogoPath)
            {
                TryDeleteLogoFile(oldLogoPath);
            }

            layout.logo_path = newLogoPath;

            layout.signature_placement = vm.Layout.signature_placement ?? "none";

            await _db.SaveChangesAsync();

            if (ownerType == "reportbuilder")
            {
                var report = await _db.ReportDefinitions.FindAsync(reportId);
                if (report != null && report.excel_layout_id != layout.ID)
                {
                    report.excel_layout_id = layout.ID;
                    await _db.SaveChangesAsync();
                }
            }

            // Replace sheets + sections
            var oldSheets = _db.MailReportExcelSheets.Where(s => s.layout_id == layout.ID);
            _db.MailReportExcelSheets.RemoveRange(oldSheets);
            await _db.SaveChangesAsync();

            foreach (var (sheetVM, si) in vm.Sheets.Select((s, i) => (s, i)))
            {
                var sheet = new MailReportExcelSheet
                {
                    layout_id = layout.ID,
                    sheet_name = sheetVM.Sheet.sheet_name,
                    sort_order = si
                };
                _db.MailReportExcelSheets.Add(sheet);
                await _db.SaveChangesAsync();

                foreach (var (sec, secIdx) in sheetVM.Sections.Select((s, i) => (s, i)))
                {
                    sec.ID = 0;
                    sec.sheet_id = sheet.ID;
                    sec.sort_order = secIdx;
                    _db.MailReportExcelSections.Add(sec);
                }
            }

            // Replace signatures
            var oldSignatures = _db.MailReportSignatures.Where(s => s.layout_id == layout.ID);
            _db.MailReportSignatures.RemoveRange(oldSignatures);
            await _db.SaveChangesAsync();

            foreach (var (sig, idx) in vm.Signatures.Select((s, i) => (s, i)))
            {
                sig.ID = 0;
                sig.layout_id = layout.ID;
                sig.sort_order = idx;
                _db.MailReportSignatures.Add(sig);
            }

            await _db.SaveChangesAsync();
        }


        private void TryDeleteLogoFile(string relativePath)
        {
            try
            {
                var relative = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var physicalPath = Path.Combine(_env.WebRootPath, relative);
                if (File.Exists(physicalPath))
                    File.Delete(physicalPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gagal hapus file logo lama: {Path}", relativePath);
            }
        }
    }
}