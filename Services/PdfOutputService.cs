using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TMSBilling.Models;

namespace TMSBilling.Services
{
    public class PdfOutputService : ReportOutputBase, IReportOutputService
    {
        public string Format => "pdf";

        public PdfOutputService(IConfiguration config, ILogger<PdfOutputService> logger)
            : base(config, logger) { }

        // Struktur internal hasil pre-fetch (semua query dijalankan dulu,
        // karena QuestPDF Compose callback-nya synchronous)
        private class GroupData
        {
            public bool SideBySide;
            public int SideGap;
            public int BottomGap;
            public List<(MailReportExcelSection Sec, DataTable Dt)> Items = new();
        }

        private class SheetData
        {
            public string SheetName = "";
            public List<GroupData> Groups = new();
        }

        public async Task<ReportOutputResult> GenerateAsync(
            ReportDefinition report,
            List<ReportParam> paramDefs,
            Dictionary<string, string> paramValues,
            MailReportExcelLayoutVM? layout)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int rowCount = 0;

            var sheetsData = new List<SheetData>();
            var hasLayout = layout != null && layout.Layout.use_custom_layout == 1;

            if (hasLayout)
            {
                foreach (var sheetVM in layout!.Sheets.OrderBy(s => s.Sheet.sort_order))
                {
                    var sheetData = new SheetData { SheetName = sheetVM.Sheet.sheet_name };
                    var groups = GroupSections(sheetVM.Sections.OrderBy(s => s.sort_order).ToList());

                    foreach (var group in groups)
                    {
                        var gd = new GroupData
                        {
                            SideBySide = group.Count > 1 && group[0].layout == "side_by_side",
                            SideGap = group.Max(g => g.side_gap > 0 ? g.side_gap : 1),
                            BottomGap = group[0].bottom_gap > 0 ? group[0].bottom_gap : 2
                        };

                        foreach (var sec in group)
                        {
                            var dt = await RunQueryAsync(sec.sql_query!, sec.sql_where, paramValues, sec.section_label);
                            rowCount += dt.Rows.Count;
                            gd.Items.Add((sec, dt));
                        }
                        sheetData.Groups.Add(gd);
                    }
                    sheetsData.Add(sheetData);
                }
            }

            var reportTitle = Resolve(layout?.Layout.report_title ?? report.report_name, paramValues);
            var reportSubtitle = Resolve(layout?.Layout.report_subtitle ?? "", paramValues);
            var titleBgHex = "#" + (string.IsNullOrWhiteSpace(layout?.Layout.title_bg_color) ? "FFFFFF" : layout!.Layout.title_bg_color);
            var titleFgHex = "#" + (string.IsNullOrWhiteSpace(layout?.Layout.title_font_color) ? "000000" : layout!.Layout.title_font_color);
            var titleFontSize = layout?.Layout.title_font_size > 0 ? layout.Layout.title_font_size : 14;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        if (!string.IsNullOrWhiteSpace(reportTitle))
                        {
                            var titleItem = col.Item();
                            if (titleBgHex != "#FFFFFF") titleItem = titleItem.Background(titleBgHex);
                            titleItem.Padding(6).Text(reportTitle).FontSize(titleFontSize).Bold().FontColor(titleFgHex);
                        }
                        if (!string.IsNullOrWhiteSpace(reportSubtitle))
                            col.Item().PaddingTop(2).Text(reportSubtitle).FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        if (!hasLayout || !sheetsData.Any())
                        {
                            col.Item().Text("No layout configured. Please set Excel Layout in Report Builder.")
                                .Italic().FontColor(Colors.Orange.Medium);
                            return;
                        }

                        bool firstSheet = true;
                        foreach (var sheet in sheetsData)
                        {
                            if (!firstSheet) col.Item().PaddingTop(14);
                            firstSheet = false;

                            col.Item().Text($"Sheet: {sheet.SheetName}").FontSize(12).Bold();

                            foreach (var group in sheet.Groups)
                            {
                                if (!group.SideBySide)
                                {
                                    foreach (var (sec, dt) in group.Items)
                                        col.Item().PaddingTop(8).Element(c => ComposeSection(c, sec, dt, paramValues));
                                }
                                else
                                {
                                    col.Item().PaddingTop(8).Row(row =>
                                    {
                                        row.Spacing(group.SideGap * 8);
                                        foreach (var (sec, dt) in group.Items)
                                        {
                                            var colCount = Math.Max(GetVisibleCols(sec.visible_columns, dt).Count, 1);
                                            row.RelativeItem(colCount).Element(c => ComposeSection(c, sec, dt, paramValues));
                                        }
                                    });
                                }
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            var bytes = document.GeneratePdf();
            sw.Stop();

            return new ReportOutputResult
            {
                Data = bytes,
                FileName = BuildFileName(report, paramValues, "pdf"),
                ContentType = "application/pdf",
                RowCount = hasLayout ? rowCount : 0,
                DurationMs = (int)sw.ElapsedMilliseconds
            };
        }

        // ── Render 1 section (title + table/key-value + grand total) ──
        private void ComposeSection(
            IContainer container,
            MailReportExcelSection sec,
            DataTable dt,
            Dictionary<string, string> paramValues)
        {
            var titleBg = "#" + (string.IsNullOrWhiteSpace(sec.title_bg_color) ? "FFD700" : sec.title_bg_color);
            var titleFg = "#" + (string.IsNullOrWhiteSpace(sec.title_font_color) ? "000000" : sec.title_font_color);
            var headerBg = "#" + (string.IsNullOrWhiteSpace(sec.header_bg_color) ? "FFD700" : sec.header_bg_color);
            var headerFg = "#" + (string.IsNullOrWhiteSpace(sec.header_font_color) ? "000000" : sec.header_font_color);
            var totalBg = "#" + (string.IsNullOrWhiteSpace(sec.total_bg_color) ? "FFD700" : sec.total_bg_color);

            container.Column(col =>
            {
                if (!string.IsNullOrWhiteSpace(sec.section_title))
                {
                    var titleText = ResolveWithDataTable(sec.section_title, dt, paramValues);
                    col.Item().Background(titleBg).Padding(4)
                        .Text(titleText).Bold().FontColor(titleFg);
                }

                if (dt.Rows.Count == 0)
                {
                    col.Item().PaddingTop(2).Text("Tidak ada data.").Italic().FontColor(Colors.Grey.Medium);
                    return;
                }

                var visibleCols = GetVisibleCols(sec.visible_columns, dt);
                if (!visibleCols.Any()) return;

                if (sec.display_mode == "KEY_VALUE")
                {
                    var row0 = dt.Rows[0];
                    col.Item().PaddingTop(2).Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                        foreach (var colName in visibleCols)
                        {
                            table.Cell().Background("#F7F9FC").Padding(3).Text(colName).Bold();
                            table.Cell().Padding(3).Text(FormatCell(row0[colName], dt.Columns[colName]!));
                        }
                    });
                    return;
                }

                // TABLE mode
                var numericTypes = new[] { typeof(int), typeof(long), typeof(short), typeof(byte),
                                           typeof(decimal), typeof(double), typeof(float) };
                var numericCols = visibleCols
                    .Where(c => dt.Columns.Contains(c) && numericTypes.Contains(dt.Columns[c]!.DataType))
                    .ToHashSet();

                var totals = new Dictionary<string, decimal>();

                col.Item().PaddingTop(2).Table(table =>
                {
                    table.ColumnsDefinition(c => { foreach (var _ in visibleCols) c.RelativeColumn(); });

                    table.Header(h =>
                    {
                        foreach (var colName in visibleCols)
                            h.Cell().Background(headerBg).Padding(4).Text(colName).Bold().FontColor(headerFg);
                    });

                    int rIdx = 0;
                    foreach (DataRow dr in dt.Rows)
                    {
                        var zebra = rIdx % 2 == 1;
                        foreach (var colName in visibleCols)
                        {
                            var cellVal = dt.Columns.Contains(colName) ? dr[colName] : DBNull.Value;
                            var colDef = dt.Columns.Contains(colName) ? dt.Columns[colName]! : new DataColumn();

                            var text = numericCols.Contains(colName) && cellVal != DBNull.Value
                                ? Convert.ToDecimal(cellVal).ToString("N2")
                                : FormatCell(cellVal, colDef);

                            if (numericCols.Contains(colName) && sec.show_grand_total == 1 && cellVal != DBNull.Value)
                            {
                                if (!totals.ContainsKey(colName)) totals[colName] = 0;
                                totals[colName] += Convert.ToDecimal(cellVal);
                            }

                            // ── FIX: jangan reassign var cell dari table.Cell() ke Background(). ──
                            // Cell() dipanggil sekali, lalu style diaplikasikan ke IContainer terpisah.
                            IContainer cellContainer = table.Cell();
                            if (zebra) cellContainer = cellContainer.Background("#F7F9FC");

                            cellContainer
                                .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .Padding(3)
                                .AlignRight_IfNumeric(numericCols.Contains(colName))
                                .Text(text);
                        }
                        rIdx++;
                    }

                    if (sec.show_grand_total == 1)
                    {
                        var label = sec.grand_total_label ?? "TOTAL";
                        bool labelPlaced = false;
                        foreach (var colName in visibleCols)
                        {
                            var cell = table.Cell().Background(totalBg).Padding(4);
                            if (!labelPlaced && !numericCols.Contains(colName))
                            {
                                cell.Text(label).Bold();
                                labelPlaced = true;
                            }
                            else if (numericCols.Contains(colName))
                            {
                                if (!labelPlaced) { cell.Text(label).Bold(); labelPlaced = true; }
                                else if (totals.TryGetValue(colName, out var t))
                                    cell.AlignRight().Text(t.ToString("N2")).Bold();
                                else
                                    cell.Text("");
                            }
                            else
                            {
                                cell.Text("");
                            }
                        }
                    }
                });
            });
        }

        // ── sama persis dengan GroupSections di ExcelLayoutService ──
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

        // ── sama seperti ResolveWithDataTable di ExcelLayoutService ──
        private string ResolveWithDataTable(string template, DataTable dt, Dictionary<string, string> paramValues)
        {
            if (string.IsNullOrEmpty(template)) return template;

            var merged = new Dictionary<string, string>(paramValues, StringComparer.OrdinalIgnoreCase);
            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                foreach (DataColumn c in dt.Columns)
                    merged[c.ColumnName] = row[c] == DBNull.Value ? "" : row[c].ToString()!;
            }
            return Regex.Replace(Resolve(template, merged), @"\{\{(\w+)\}\}", m =>
                merged.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);
        }
    }

    // Extension kecil untuk alignment kondisional biar kode di atas lebih ringkas
    internal static class QuestPdfExt
    {
        public static QuestPDF.Infrastructure.IContainer AlignRight_IfNumeric(
            this QuestPDF.Infrastructure.IContainer container, bool isNumeric)
            => isNumeric ? container.AlignRight() : container;
    }
}