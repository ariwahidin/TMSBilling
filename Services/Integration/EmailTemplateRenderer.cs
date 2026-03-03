using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMSBilling.Models;

namespace TMSBilling.Services.Integration
{
    /// <summary>
    /// Context lengkap untuk render email template.
    /// </summary>
    public class EmailRenderContext
    {
        public TMSBilling.Models.Integration Integration { get; set; } = null!;
        public SenderResult Result { get; set; } = null!;

        /// <summary>Event data (single row dari event payload)</summary>
        public Dictionary<string, object?> EventData { get; set; } = new();

        /// <summary>
        /// Semua rows yang dikirim ke channel (hasil query atau event).
        /// Digunakan untuk render {{__data_table__}}.
        /// </summary>
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
    }

    /// <summary>
    /// Render email subject, body, footer dengan support:
    /// - Placeholder {{field}} dari event data dan system variables
    /// - Placeholder {{__data_table__}} untuk render tabel HTML semua rows
    /// - Placeholder {{__summary__}} untuk ringkasan eksekusi
    /// - Default template jika user tidak mengisi
    /// </summary>
    public class EmailTemplateRenderer
    {
        // ── System placeholders ───────────────────────────────────────────────

        private static Dictionary<string, object?> BuildSystemVars(EmailRenderContext ctx)
        {
            var isSuccess = ctx.Result.Success;
            return new Dictionary<string, object?>
            {
                ["integration_name"]  = ctx.Integration.Name,
                ["integration_id"]    = ctx.Integration.Id,
                ["event_key"]         = ctx.Integration.EventKey,
                ["channel_type"]      = ctx.Integration.ChannelType,
                ["source_type"]       = ctx.Integration.SourceType,
                ["timing"]            = ctx.Integration.Timing,
                ["status"]            = isSuccess ? "SUCCESS" : "FAILED",
                ["status_id"]         = isSuccess ? "BERHASIL" : "GAGAL",
                ["message"]           = ctx.Result.Message ?? string.Empty,
                ["error_detail"]      = ctx.Result.ErrorDetail ?? string.Empty,
                ["row_count"]         = ctx.Result.RowCount,
                ["duration_ms"]       = ctx.Result.DurationMs,
                ["duration_sec"]      = (ctx.Result.DurationMs / 1000.0).ToString("F2"),
                ["datetime"]          = DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss"),
                ["date"]              = DateTime.Now.ToString("dd MMMM yyyy"),
                ["time"]              = DateTime.Now.ToString("HH:mm:ss"),
                ["year"]              = DateTime.Now.Year,
                ["month"]             = DateTime.Now.ToString("MMMM yyyy"),
            };
        }

        // ── Public: Render Subject ────────────────────────────────────────────

        public string RenderSubject(EmailRenderContext ctx)
        {
            var template = ctx.Integration.EmailSubjectTemplate;

            if (string.IsNullOrWhiteSpace(template))
            {
                template = ctx.Result.Success
                    ? "[TMS] ✅ {{integration_name}} — {{row_count}} data berhasil dikirim"
                    : "[TMS] ❌ {{integration_name}} — Eksekusi gagal";
            }

            return ResolvePlaceholders(template, ctx);
        }

        // ── Public: Render Full HTML Email ────────────────────────────────────

        public string RenderHtml(EmailRenderContext ctx)
        {
            var bodyContent = RenderBodyContent(ctx);
            var footerContent = RenderFooter(ctx);
            return WrapInEmailLayout(bodyContent, footerContent, ctx.Result.Success);
        }

        // ── Public: Get all available placeholder list ────────────────────────

        public List<PlaceholderInfo> GetAvailablePlaceholders(EmailRenderContext ctx)
        {
            var list = new List<PlaceholderInfo>
            {
                // System
                new("integration_name",  "Nama integrasi",             "Order Loaded → Google Sheets",  "system"),
                new("event_key",         "Event key",                   "order.loaded",                  "system"),
                new("channel_type",      "Tipe channel",               "google_sheets",                  "system"),
                new("status",            "Status (EN)",                 "SUCCESS / FAILED",               "system"),
                new("status_id",         "Status (ID)",                 "BERHASIL / GAGAL",               "system"),
                new("message",           "Pesan hasil eksekusi",        "3 baris berhasil diproses",      "system"),
                new("row_count",         "Jumlah baris diproses",       "3",                              "system"),
                new("duration_ms",       "Durasi (millisecond)",        "342",                            "system"),
                new("duration_sec",      "Durasi (detik)",              "0.34",                           "system"),
                new("datetime",          "Tanggal dan waktu",           "27 Februari 2026 14:30:00",      "system"),
                new("date",              "Tanggal saja",                "27 Februari 2026",               "system"),
                new("time",              "Waktu saja",                  "14:30:00",                       "system"),

                // Special
                new("__data_table__",    "Tabel HTML semua data rows",  "(render otomatis)",              "special"),
                new("__summary__",       "Ringkasan eksekusi",          "(render otomatis)",              "special"),
                new("__error_box__",     "Kotak error (hanya jika gagal)", "(render otomatis)",           "special"),
            };

            // Dynamic dari event data
            foreach (var (key, val) in ctx.EventData)
            {
                if (!list.Any(p => p.Key == key))
                    list.Add(new PlaceholderInfo(key, $"Data: {key}", val?.ToString() ?? "(null)", "data"));
            }

            // Dynamic dari kolom query rows
            if (ctx.Rows.Any())
            {
                foreach (var col in ctx.Rows[0].Keys)
                {
                    if (!list.Any(p => p.Key == col))
                    {
                        var sampleVal = ctx.Rows[0].GetValueOrDefault(col)?.ToString() ?? "(null)";
                        list.Add(new PlaceholderInfo(col, $"Kolom: {col}", sampleVal, "data"));
                    }
                }
            }

            return list;
        }

        // ── Render Body Content ───────────────────────────────────────────────

        private string RenderBodyContent(EmailRenderContext ctx)
        {
            // Kalau user sudah isi custom body template
            if (!string.IsNullOrWhiteSpace(ctx.Integration.EmailBodyTemplate))
            {
                var customBody = ResolvePlaceholders(ctx.Integration.EmailBodyTemplate, ctx);
                customBody = RenderSpecialBlocks(customBody, ctx);
                return customBody;
            }

            // Default template otomatis
            return BuildDefaultBody(ctx);
        }

        private string RenderFooter(EmailRenderContext ctx)
        {
            if (!string.IsNullOrWhiteSpace(ctx.Integration.EmailFooterTemplate))
                return ResolvePlaceholders(ctx.Integration.EmailFooterTemplate, ctx);

            return BuildDefaultFooter();
        }

        // ── Default Body Template ─────────────────────────────────────────────

        private string BuildDefaultBody(EmailRenderContext ctx)
        {
            var isSuccess = ctx.Result.Success;
            var statusColor = isSuccess ? "#16a34a" : "#dc2626";
            var statusText = isSuccess ? "BERHASIL" : "GAGAL";
            var statusIcon = isSuccess ? "✅" : "❌";

            var sb = new StringBuilder();

            // Greeting
            sb.Append($@"
            <p style='margin:0 0 16px 0; font-size:14px; color:#374151;'>
                Berikut adalah laporan eksekusi integrasi dari <strong>TMS Billing Integration Hub</strong>.
            </p>");

            // Status badge
            sb.Append($@"
            <div style='display:inline-block; background:{statusColor}; color:#fff;
                        padding:6px 18px; border-radius:20px; font-size:13px;
                        font-weight:bold; margin-bottom:20px; letter-spacing:0.5px;'>
                {statusIcon} {statusText}
            </div>");

            // Info table
            sb.Append(@"
            <table style='width:100%; border-collapse:collapse; font-size:13px; margin-bottom:20px;'>
                <colgroup><col style='width:160px'/><col/></colgroup>");

            void AddRow(string label, string value, string? valueColor = null)
            {
                var style = valueColor != null ? $"color:{valueColor}; font-weight:600;" : "color:#111827;";
                sb.Append($@"
                <tr>
                    <td style='padding:7px 12px; background:#f9fafb; border:1px solid #e5e7eb;
                               color:#6b7280; font-weight:500;'>{label}</td>
                    <td style='padding:7px 12px; border:1px solid #e5e7eb; {style}'>{value}</td>
                </tr>");
            }

            AddRow("Integrasi", ctx.Integration.Name);
            AddRow("Event Key", $"<code style='background:#f3f4f6; padding:2px 6px; border-radius:3px;'>{ctx.Integration.EventKey}</code>");
            AddRow("Channel", ctx.Integration.ChannelType.Replace("_", " ").ToUpper());
            AddRow("Status", $"{statusIcon} {statusText}", statusColor);
            AddRow("Waktu", DateTime.Now.ToString("dd MMMM yyyy, HH:mm:ss"));
            AddRow("Jumlah Data", $"{ctx.Result.RowCount:N0} baris");
            AddRow("Durasi", $"{ctx.Result.DurationMs:N0} ms ({ctx.Result.DurationMs / 1000.0:F2} detik)");

            if (!string.IsNullOrWhiteSpace(ctx.Result.Message))
                AddRow("Pesan", ctx.Result.Message);

            sb.Append("</table>");

            // Error box (jika gagal)
            if (!isSuccess && !string.IsNullOrWhiteSpace(ctx.Result.ErrorDetail))
                sb.Append(BuildErrorBox(ctx.Result.ErrorDetail));

            // Data table (jika ada rows dan opsi diaktifkan)
            if (ctx.Integration.EmailIncludeDataTable && ctx.Rows.Any())
                sb.Append(BuildDataTable(ctx.Rows));

            return sb.ToString();
        }

        // ── Special Blocks ────────────────────────────────────────────────────

        /// <summary>
        /// Ganti {{__data_table__}}, {{__summary__}}, {{__error_box__}}
        /// di custom template dengan HTML yang dirender.
        /// </summary>
        private string RenderSpecialBlocks(string html, EmailRenderContext ctx)
        {
            // {{__data_table__}}
            if (html.Contains("{{__data_table__}}"))
            {
                var tableHtml = ctx.Rows.Any() ? BuildDataTable(ctx.Rows) : "<p style='color:#9ca3af;font-size:12px;'>Tidak ada data.</p>";
                html = html.Replace("{{__data_table__}}", tableHtml);
            }

            // {{__summary__}}
            if (html.Contains("{{__summary__}}"))
                html = html.Replace("{{__summary__}}", BuildSummaryBlock(ctx));

            // {{__error_box__}}
            if (html.Contains("{{__error_box__}}"))
            {
                var errorHtml = (!ctx.Result.Success && !string.IsNullOrWhiteSpace(ctx.Result.ErrorDetail))
                    ? BuildErrorBox(ctx.Result.ErrorDetail)
                    : string.Empty;
                html = html.Replace("{{__error_box__}}", errorHtml);
            }

            return html;
        }

        // ── Data Table HTML ───────────────────────────────────────────────────

        private string BuildDataTable(List<Dictionary<string, object?>> rows)
        {
            if (!rows.Any()) return string.Empty;

            var columns = rows[0].Keys.ToList();
            var sb = new StringBuilder();

            sb.Append($@"
            <div style='margin-top:20px;'>
                <p style='font-size:13px; font-weight:600; color:#374151; margin-bottom:8px;'>
                    📋 Data ({rows.Count:N0} baris)
                </p>
                <div style='overflow-x:auto;'>
                <table style='width:100%; border-collapse:collapse; font-size:12px; min-width:600px;'>
                    <thead>
                        <tr style='background:#1e40af; color:#fff;'>");

            foreach (var col in columns)
                sb.Append($"<th style='padding:8px 10px; text-align:left; white-space:nowrap; font-weight:600;'>{HtmlEncode(col)}</th>");

            sb.Append("</tr></thead><tbody>");

            for (int i = 0; i < rows.Count; i++)
            {
                var bg = i % 2 == 0 ? "#ffffff" : "#f8fafc";
                sb.Append($"<tr style='background:{bg};'>");
                foreach (var col in columns)
                {
                    var val = rows[i].GetValueOrDefault(col)?.ToString() ?? string.Empty;
                    sb.Append($"<td style='padding:7px 10px; border-bottom:1px solid #e5e7eb; white-space:nowrap;'>{HtmlEncode(val)}</td>");
                }
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table></div></div>");
            return sb.ToString();
        }

        // ── Summary Block ─────────────────────────────────────────────────────

        private string BuildSummaryBlock(EmailRenderContext ctx)
        {
            var isSuccess = ctx.Result.Success;
            var color = isSuccess ? "#16a34a" : "#dc2626";
            return $@"
            <div style='background:#f0fdf4; border-left:4px solid {color};
                        padding:12px 16px; border-radius:0 6px 6px 0; margin:16px 0; font-size:13px;'>
                <strong style='color:{color};'>{(isSuccess ? "✅ Sukses" : "❌ Gagal")}</strong> —
                {ctx.Result.RowCount:N0} baris diproses dalam {ctx.Result.DurationMs:N0}ms
                pada {DateTime.Now:dd MMM yyyy HH:mm:ss}
            </div>";
        }

        // ── Error Box ─────────────────────────────────────────────────────────

        private string BuildErrorBox(string errorDetail)
        {
            // Truncate if too long for email
            var truncated = errorDetail.Length > 1000
                ? errorDetail.Substring(0, 1000) + "...(truncated)"
                : errorDetail;

            return $@"
            <div style='margin:16px 0; border:1px solid #fecaca; border-radius:6px; overflow:hidden;'>
                <div style='background:#fef2f2; padding:8px 12px; border-bottom:1px solid #fecaca;'>
                    <strong style='color:#dc2626; font-size:12px;'>⚠️ Error Detail</strong>
                </div>
                <pre style='margin:0; padding:12px; background:#fff5f5; font-size:11px;
                            color:#7f1d1d; overflow-x:auto; white-space:pre-wrap;
                            word-break:break-all;'>{HtmlEncode(truncated)}</pre>
            </div>";
        }

        // ── Default Footer ────────────────────────────────────────────────────

        private string BuildDefaultFooter()
        {
            return $@"
            <p style='margin:0; font-size:11px; color:#9ca3af; text-align:center;'>
                Email ini dikirim otomatis oleh <strong>TMS Billing Integration Hub</strong>
                pada {DateTime.Now:dd MMM yyyy HH:mm:ss}.<br/>
                Jangan balas email ini. Jika ada pertanyaan, hubungi administrator sistem.
            </p>";
        }

        // ── Layout Wrapper ────────────────────────────────────────────────────

        private string WrapInEmailLayout(string bodyContent, string footerContent, bool isSuccess)
        {
            var headerColor = isSuccess ? "#1e40af" : "#dc2626";
            var headerText = isSuccess ? "Integration Hub — Notifikasi Sukses" : "Integration Hub — Notifikasi Gagal";

            return $@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1'>
</head>
<body style='margin:0; padding:0; background:#f3f4f6; font-family:""Segoe UI"",Arial,sans-serif;'>
<table width='100%' cellpadding='0' cellspacing='0' style='background:#f3f4f6; padding:32px 16px;'>
<tr><td align='center'>
<table width='640' cellpadding='0' cellspacing='0'
       style='max-width:640px; width:100%; background:#ffffff; border-radius:10px;
              box-shadow:0 1px 3px rgba(0,0,0,0.08); overflow:hidden;'>

    <!-- Header -->
    <tr>
        <td style='background:{headerColor}; padding:20px 28px;'>
            <table width='100%' cellpadding='0' cellspacing='0'>
                <tr>
                    <td>
                        <p style='margin:0; font-size:16px; font-weight:700; color:#fff;'>
                            📡 {headerText}
                        </p>
                    </td>
                    <td align='right'>
                        <img src='https://via.placeholder.com/80x24/ffffff/1e40af?text=TMS+Billing'
                             alt='TMS Billing' style='height:24px; opacity:0.9;'>
                    </td>
                </tr>
            </table>
        </td>
    </tr>

    <!-- Body -->
    <tr>
        <td style='padding:24px 28px;'>
            {bodyContent}
        </td>
    </tr>

    <!-- Divider -->
    <tr>
        <td style='padding:0 28px;'>
            <hr style='border:none; border-top:1px solid #e5e7eb; margin:0;'>
        </td>
    </tr>

    <!-- Footer -->
    <tr>
        <td style='padding:16px 28px 24px 28px; background:#f9fafb;'>
            {footerContent}
        </td>
    </tr>

</table>
</td></tr>
</table>
</body>
</html>";
        }

        // ── Placeholder Resolution ────────────────────────────────────────────

        private string ResolvePlaceholders(string template, EmailRenderContext ctx)
        {
            if (string.IsNullOrWhiteSpace(template)) return template;

            // Merge: system vars + event data (event data bisa override system jika nama sama)
            var allVars = BuildSystemVars(ctx);
            foreach (var (key, val) in ctx.EventData)
                allVars.TryAdd(key.ToLower().Replace(" ", "_"), val);

            // Jika ada rows, tambahkan kolom dari baris pertama juga
            if (ctx.Rows.Any())
                foreach (var (key, val) in ctx.Rows[0])
                    allVars.TryAdd(key.ToLower().Replace(" ", "_"), val);

            return Regex.Replace(template, @"\{\{(\w+)\}\}", match =>
            {
                var key = match.Groups[1].Value.ToLower();

                // Special blocks dihandle di RenderSpecialBlocks, skip di sini
                if (key.StartsWith("__")) return match.Value;

                if (allVars.TryGetValue(key, out var val))
                    return val?.ToString() ?? string.Empty;

                return string.Empty; // kosongkan placeholder yang tidak ditemukan
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string HtmlEncode(string? s) =>
            System.Net.WebUtility.HtmlEncode(s ?? string.Empty);
    }

    // ── DTO untuk placeholder list di UI ─────────────────────────────────────
    public record PlaceholderInfo(string Key, string Description, string Example, string Category);
}
