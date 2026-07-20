using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
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

        private readonly IWebHostEnvironment _env;

        // ── Tuning konstanta autofit ──────────────────────────
        private const float CharWidthPt = 5.3f;
        private const float MinColWidth = 50f;
        private const float MaxColWidth = 220f;
        private const float HeaderPad = 16f;
        private const float PageMargin = 25f;
        private const float SideGapUnitPt = 14f;

        public PdfOutputService(IConfiguration config, ILogger<PdfOutputService> logger, IWebHostEnvironment env)
            : base(config, logger)
        {
            _env = env;
        }

        private class SectionRenderInfo
        {
            public MailReportExcelSection Sec = null!;
            public DataTable Dt = null!;
            public List<string> VisibleCols = new();
            public Dictionary<string, float> ColWidths = new();
            public float LabelColWidth;
            public float ValueColWidth;
            public float TotalWidth;
        }

        private class GroupRenderInfo
        {
            public bool SideBySide;
            public int SideGapPt;
            public int BottomGap;
            public List<SectionRenderInfo> Items = new();
            public float TotalWidth;
        }

        private class SheetRenderInfo
        {
            public string SheetName = "";
            public List<GroupRenderInfo> Groups = new();
            public float MaxWidth;
        }

        public async Task<ReportOutputResult> GenerateAsync(
            ReportDefinition report,
            List<ReportParam> paramDefs,
            Dictionary<string, string> paramValues,
            MailReportExcelLayoutVM? layout)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int rowCount = 0;

            var sheetsData = new List<SheetRenderInfo>();
            var hasLayout = layout != null && layout.Layout.use_custom_layout == 1;

            if (hasLayout)
            {
                foreach (var sheetVM in layout!.Sheets.OrderBy(s => s.Sheet.sort_order))
                {
                    var sheetInfo = new SheetRenderInfo { SheetName = sheetVM.Sheet.sheet_name };
                    var groups = GroupSections(sheetVM.Sections.OrderBy(s => s.sort_order).ToList());

                    foreach (var group in groups)
                    {
                        var gInfo = new GroupRenderInfo
                        {
                            SideBySide = group.Count > 1 && group[0].layout == "side_by_side",
                            //SideGapPt = (int)((group.Max(g => g.side_gap > 0 ? g.side_gap : 1)) * SideGapUnitPt),
                            SideGapPt = (int)((group.Max(g => Math.Max(g.side_gap, 0))) * SideGapUnitPt),
                            BottomGap = group[0].bottom_gap > 0 ? group[0].bottom_gap : 2
                        };

                        foreach (var sec in group)
                        {
                            var dt = await RunQueryAsync(sec.sql_query!, sec.sql_where, paramValues, sec.section_label);
                            rowCount += dt.Rows.Count;

                            var visibleCols = GetVisibleCols(sec.visible_columns, dt);
                            var secInfo = new SectionRenderInfo { Sec = sec, Dt = dt, VisibleCols = visibleCols };

                            if (sec.display_mode == "KEY_VALUE")
                            {
                                var maxLabelLen = visibleCols.Any() ? visibleCols.Max(c => c.Length) : 5;
                                var maxValLen = 5;
                                if (dt.Rows.Count > 0)
                                {
                                    var row0 = dt.Rows[0];
                                    foreach (var c in visibleCols)
                                    {
                                        if (!dt.Columns.Contains(c)) continue;
                                        var text = FormatCell(row0[c], dt.Columns[c]!);
                                        if (text.Length > maxValLen) maxValLen = text.Length;
                                    }
                                }
                                secInfo.LabelColWidth = Clamp(maxLabelLen * CharWidthPt + HeaderPad);
                                secInfo.ValueColWidth = Clamp(maxValLen * CharWidthPt + HeaderPad);
                                secInfo.TotalWidth = secInfo.LabelColWidth + secInfo.ValueColWidth;
                            }
                            else
                            {
                                secInfo.ColWidths = ComputeColumnWidths(visibleCols, dt);
                                secInfo.TotalWidth = secInfo.ColWidths.Values.DefaultIfEmpty(MinColWidth).Sum();
                            }

                            if (secInfo.TotalWidth < 150) secInfo.TotalWidth = 150;

                            gInfo.Items.Add(secInfo);
                        }

                        gInfo.TotalWidth = gInfo.SideBySide
                            ? gInfo.Items.Sum(i => i.TotalWidth) + gInfo.SideGapPt * Math.Max(gInfo.Items.Count - 1, 0)
                            : gInfo.Items.Max(i => i.TotalWidth);

                        sheetInfo.Groups.Add(gInfo);
                    }

                    sheetInfo.MaxWidth = sheetInfo.Groups.Any() ? sheetInfo.Groups.Max(g => g.TotalWidth) : 400;
                    sheetsData.Add(sheetInfo);
                }
            }

            var reportTitle = Resolve(layout?.Layout.report_title ?? report.report_name, paramValues);
            var reportSubtitle = Resolve(layout?.Layout.report_subtitle ?? "", paramValues);
            var titleBgHex = "#" + (string.IsNullOrWhiteSpace(layout?.Layout.title_bg_color) ? "FFFFFF" : layout!.Layout.title_bg_color);
            var titleFgHex = "#" + (string.IsNullOrWhiteSpace(layout?.Layout.title_font_color) ? "000000" : layout!.Layout.title_font_color);
            var titleFontSize = layout?.Layout.title_font_size > 0 ? layout.Layout.title_font_size : 14;

            // ── Load logo (kalau ada) ────────────────────────────
            byte[]? logoBytes = null;
            if (!string.IsNullOrWhiteSpace(layout?.Layout.logo_path))
            {
                try
                {
                    var relative = layout!.Layout.logo_path!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var physicalPath = Path.Combine(_env.WebRootPath, relative);
                    if (File.Exists(physicalPath))
                        logoBytes = await File.ReadAllBytesAsync(physicalPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "PdfOutputService: gagal load logo dari {Path}", layout?.Layout.logo_path);
                }
            }

            var signatures = layout?.Signatures ?? new List<MailReportSignature>();
            var sigPlacement = layout?.Layout.signature_placement ?? "none";

            // ── Hitung ukuran halaman dinamis ──────────────────
            var baseLandscape = PageSizes.A4.Landscape();
            float pageHeight = baseLandscape.Height;
            float contentWidth = sheetsData.Any() ? sheetsData.Max(s => s.MaxWidth) : baseLandscape.Width - PageMargin * 2;
            float pageWidth = Math.Max(baseLandscape.Width, contentWidth + PageMargin * 2);

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(new PageSize(pageWidth, pageHeight));
                    page.Margin(PageMargin);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // ── HEADER: logo + title ─────────────────────
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            if (logoBytes != null)
                                row.ConstantItem(60).Height(60).Image(logoBytes).FitArea();

                            row.RelativeItem().Column(inner =>
                            {
                                if (!string.IsNullOrWhiteSpace(reportTitle))
                                {
                                    var titleItem = inner.Item();
                                    if (titleBgHex != "#FFFFFF")
                                        titleItem.Background(titleBgHex).Padding(6).Text(reportTitle).FontSize(titleFontSize).Bold().FontColor(titleFgHex);
                                    else
                                        titleItem.Padding(6).Text(reportTitle).FontSize(titleFontSize).Bold().FontColor(titleFgHex);
                                }
                                if (!string.IsNullOrWhiteSpace(reportSubtitle))
                                    inner.Item().PaddingTop(2).Text(reportSubtitle).FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });

                    // ── CONTENT ───────────────────────────────────
                    page.Content().PaddingTop(10).Column(col =>
                    {
                        if (!hasLayout || !sheetsData.Any())
                        {
                            col.Item().Text("No layout configured. Please set Excel Layout in Report Builder.")
                                .Italic().FontColor(Colors.Orange.Medium);
                        }
                        else
                        {
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
                                        var info = group.Items[0];
                                        col.Item().PaddingTop(8).Element(c => ComposeSection(c, info, paramValues));
                                    }
                                    else
                                    {
                                        col.Item().PaddingTop(8).Row(row =>
                                        {
                                            row.Spacing(group.SideGapPt);
                                            foreach (var info in group.Items)
                                                row.ConstantItem(info.TotalWidth).Element(c => ComposeSection(c, info, paramValues));
                                        });
                                    }
                                }
                            }
                        }

                        // Signature di akhir dokumen (hanya kalau placement = last_page)
                        if (sigPlacement == "last_page" && signatures.Any())
                            col.Item().Element(c => ComposeSignatureBlock(c, signatures));
                    });

                    // ── FOOTER ────────────────────────────────────
                    page.Footer().Column(col =>
                    {
                        // Signature berulang tiap halaman kalau placement = every_page
                        if (sigPlacement == "every_page" && signatures.Any())
                            col.Item().Element(c => ComposeSignatureBlock(c, signatures));

                        col.Item().AlignCenter().Text(x =>
                        {
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
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

        // ── Blok tanda tangan, jumlah kolom dinamis ──────────────
        private void ComposeSignatureBlock(IContainer container, List<MailReportSignature> signatures)
        {
            if (signatures == null || !signatures.Any()) return;

            container.PaddingTop(30).Row(row =>
            {
                row.Spacing(20);
                foreach (var sig in signatures.OrderBy(s => s.sort_order))
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Height(50); // ruang kosong untuk tanda tangan fisik
                        col.Item().BorderTop(1).BorderColor(Colors.Black).PaddingTop(2)
                            .AlignCenter().Text(sig.label).FontSize(9);
                    });
                }
            });
        }

        // ── Render 1 section (title + table/key-value + grand total) ──
        private void ComposeSection(
            IContainer container,
            SectionRenderInfo info,
            Dictionary<string, string> paramValues)
        {
            var sec = info.Sec;
            var dt = info.Dt;
            var visibleCols = info.VisibleCols;

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
                    col.Item().Background(titleBg).Padding(4).Text(titleText).Bold().FontColor(titleFg);
                }

                if (dt.Rows.Count == 0)
                {
                    col.Item().PaddingTop(2).Text("Tidak ada data.").Italic().FontColor(Colors.Grey.Medium);
                    return;
                }

                if (!visibleCols.Any()) return;

                if (sec.display_mode == "KEY_VALUE")
                {
                    var row0 = dt.Rows[0];
                    col.Item().PaddingTop(2).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(info.LabelColWidth);
                            c.ConstantColumn(info.ValueColWidth);
                        });
                        foreach (var colName in visibleCols)
                        {
                            table.Cell().Background("#F7F9FC").Padding(3).Text(colName).Bold();
                            table.Cell().Padding(3).Text(dt.Columns.Contains(colName)
                                ? FormatCell(row0[colName], dt.Columns[colName]!)
                                : "");
                        }
                    });
                    return;
                }

                var numericTypes = new[] { typeof(int), typeof(long), typeof(short), typeof(byte),
                                           typeof(decimal), typeof(double), typeof(float) };
                var numericCols = visibleCols
                    .Where(c => dt.Columns.Contains(c) && numericTypes.Contains(dt.Columns[c]!.DataType))
                    .ToHashSet();

                var totals = new Dictionary<string, decimal>();

                col.Item().PaddingTop(2).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        foreach (var colName in visibleCols)
                            c.ConstantColumn(info.ColWidths.TryGetValue(colName, out var w) ? w : MinColWidth);
                    });

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

        private Dictionary<string, float> ComputeColumnWidths(List<string> cols, DataTable dt)
        {
            var widths = new Dictionary<string, float>();
            foreach (var col in cols)
            {
                int maxLen = col.Length;
                if (dt.Columns.Contains(col))
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var val = row[col];
                        var text = val == DBNull.Value || val == null ? "" : FormatCell(val, dt.Columns[col]!);
                        if (text.Length > maxLen) maxLen = text.Length;
                    }
                }
                widths[col] = Clamp(maxLen * CharWidthPt + HeaderPad);
            }
            return widths;
        }

        private static float Clamp(float w) => Math.Clamp(w, MinColWidth, MaxColWidth);

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

    internal static class QuestPdfExt
    {
        public static IContainer AlignRight_IfNumeric(this IContainer container, bool isNumeric)
            => isNumeric ? container.AlignRight() : container;
    }
}