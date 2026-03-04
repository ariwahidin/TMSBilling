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
        Task<bool> HasCustomLayoutAsync(int reportId);
        Task<byte[]> BuildAsync(int reportId, Dictionary<string, string> eventParams);
        Task<MailReportExcelLayoutVM> LoadForFormAsync(int reportId);
        Task SaveFromFormAsync(int reportId, MailReportExcelLayoutVM vm);
    }

    public class ExcelLayoutService : IExcelLayoutService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ExcelLayoutService> _logger;
        private readonly string _connStr;

        public ExcelLayoutService(
            AppDbContext db,
            ILogger<ExcelLayoutService> logger,
            IConfiguration config)
        {
            _db = db;
            _logger = logger;
            _connStr = config.GetConnectionString("DefaultConnection")!;
        }

        // ──────────────────────────────────────────────
        // HasCustomLayoutAsync
        // ──────────────────────────────────────────────
        public async Task<bool> HasCustomLayoutAsync(int reportId)
        {
            var layout = await _db.MailReportExcelLayouts
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.report_id == reportId);
            return layout != null && layout.use_custom_layout == 1;
        }

        // ──────────────────────────────────────────────
        // LoadForFormAsync
        // ──────────────────────────────────────────────
        public async Task<MailReportExcelLayoutVM> LoadForFormAsync(int reportId)
        {
            var layout = await _db.MailReportExcelLayouts
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.report_id == reportId);

            if (layout == null)
                return new MailReportExcelLayoutVM
                {
                    Layout = new MailReportExcelLayout { report_id = reportId },
                    Sheets = new List<MailReportExcelSheetVM>()
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

            return new MailReportExcelLayoutVM
            {
                Layout = layout,
                Sheets = sheets.Select(s => new MailReportExcelSheetVM
                {
                    Sheet = s,
                    Sections = allSections.Where(sec => sec.sheet_id == s.ID).ToList()
                }).ToList()
            };
        }

        // ──────────────────────────────────────────────
        // SaveFromFormAsync
        // ──────────────────────────────────────────────
        public async Task SaveFromFormAsync(int reportId, MailReportExcelLayoutVM vm)
        {
            // Upsert layout header
            var layout = await _db.MailReportExcelLayouts
                .FirstOrDefaultAsync(l => l.report_id == reportId);

            if (layout == null)
            {
                layout = new MailReportExcelLayout { report_id = reportId };
                _db.MailReportExcelLayouts.Add(layout);
            }

            layout.use_custom_layout = vm.Layout.use_custom_layout;
            layout.report_title = vm.Layout.report_title;
            layout.title_bg_color = vm.Layout.title_bg_color ?? "FFFFFF";
            layout.title_font_color = vm.Layout.title_font_color ?? "000000";
            layout.title_font_size = vm.Layout.title_font_size > 0 ? vm.Layout.title_font_size : 14;

            await _db.SaveChangesAsync();

            // Replace sheets + sections (cascade delete handles sections)
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

            await _db.SaveChangesAsync();
        }

        // ──────────────────────────────────────────────
        // BuildAsync — entry point utama
        // ──────────────────────────────────────────────
        public async Task<byte[]> BuildAsync(int reportId, Dictionary<string, string> eventParams)
        {
            var vm = await LoadForFormAsync(reportId);
            var resolvedTitle = Resolve(vm.Layout.report_title ?? "", eventParams);

            using var wb = new XLWorkbook();

            foreach (var sheetVM in vm.Sheets.OrderBy(s => s.Sheet.sort_order))
            {
                var sheetName = SanitizeSheetName(Resolve(sheetVM.Sheet.sheet_name, eventParams));
                var ws = wb.Worksheets.Add(sheetName);
                await BuildSheetAsync(ws, sheetVM, vm.Layout, resolvedTitle, eventParams);
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
            Dictionary<string, string> eventParams)
        {
            int currentRow = 1;

            // Report title di pojok kiri atas
            if (!string.IsNullOrWhiteSpace(resolvedTitle))
            {
                var cell = ws.Cell(currentRow, 1);
                cell.Value = resolvedTitle;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = layout.title_font_size;
                SetFontColor(cell.Style.Font, layout.title_font_color ?? "000000");
                if (!string.IsNullOrWhiteSpace(layout.title_bg_color) && layout.title_bg_color != "FFFFFF")
                    SetFill(cell.Style.Fill, layout.title_bg_color);
                currentRow += 2;
            }

            // Kelompokkan sections berdasarkan group_id + layout
            var groups = GroupSections(sheetVM.Sections.OrderBy(s => s.sort_order).ToList());

            foreach (var group in groups)
            {
                int bottomGap = group[0].bottom_gap > 0 ? group[0].bottom_gap : 2;

                if (group.Count == 1 || group[0].layout != "side_by_side")
                {
                    // Vertical
                    foreach (var sec in group)
                    {
                        var dt = await RunQueryAsync(sec, eventParams);
                        currentRow = RenderSection(ws, sec, dt, currentRow, 1, eventParams);
                        currentRow += sec.bottom_gap > 0 ? sec.bottom_gap : 2;
                    }
                }
                else
                {
                    // Side by side
                    currentRow = await RenderSideBySideAsync(ws, group, currentRow, eventParams);
                    currentRow += bottomGap;
                }
            }

            ws.Columns().AdjustToContents();
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

                var colCount = GetVisibleColumns(sec, dt).Count;
                colCount = Math.Max(colCount, 1);
                currentCol += colCount + (sec.side_gap > 0 ? sec.side_gap : 1);
            }

            return maxRow;
        }

        // ──────────────────────────────────────────────
        // RenderSection — render 1 tabel ke worksheet
        // Returns baris terakhir yang ditulis
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
                var titleText = Resolve(sec.section_title, eventParams);
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

            // Header row
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

                // Alternate row shading
                if ((row - dataRowStart) % 2 == 1)
                {
                    var rowRange = ws.Range(row, startCol, row, startCol + visibleCols.Count - 1);
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F7F9FC");
                }

                row++;
            }

            // Grand Total row
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
                        // Kolom pertama non-numerik → label
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
                return dt.TimeOfDay == TimeSpan.Zero ? dt.ToString("yyyy-MM-dd") : dt.ToString("yyyy-MM-dd HH:mm");
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
                ["month_name"] = DateTime.Now.ToString("MMMM")
            };
            return Regex.Replace(template, @"\{\{(\w+)\}\}", m =>
                merged.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);
        }

        private string InjectWhere(string sql, string where)
        {
            sql = sql.TrimEnd(';', ' ');
            var upper = Regex.Replace(sql.ToUpper(), @"\s+", " ");
            bool hasWhere = false;
            int depth = 0;
            for (int i = 0; i < upper.Length - 5; i++)
            {
                if (upper[i] == '(') depth++;
                else if (upper[i] == ')') depth--;
                else if (depth == 0 && upper.Substring(i, 5) == "WHERE")
                {
                    bool prev = i == 0 || !char.IsLetterOrDigit(upper[i - 1]);
                    bool next = i + 5 >= upper.Length || !char.IsLetterOrDigit(upper[i + 5]);
                    if (prev && next) { hasWhere = true; break; }
                }
            }
            return hasWhere ? sql + $" AND ({where})" : sql + $" WHERE {where}";
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
            catch { /* ignore invalid color */ }
        }

        private void SetFontColor(IXLFont font, string? hex)
        {
            try { if (!string.IsNullOrWhiteSpace(hex)) font.FontColor = XLColor.FromHtml("#" + hex.TrimStart('#')); }
            catch { /* ignore invalid color */ }
        }

        private string SanitizeSheetName(string name)
        {
            foreach (var c in new[] { ':', '\\', '/', '?', '*', '[', ']' }) name = name.Replace(c, '_');
            if (name.Length > 31) name = name[..31];
            return string.IsNullOrWhiteSpace(name) ? "Sheet" : name;
        }
    }
}