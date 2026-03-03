using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using TMSBilling.Models;

namespace TMSBilling.Services.Integration
{
    /// <summary>
    /// Hasil satu attachment file yang siap dilampirkan ke email.
    /// </summary>
    public class AttachmentFile
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
    }

    public interface IAttachmentBuilder
    {
        /// <summary>
        /// Build semua attachment files untuk 1 integrasi.
        /// Return list of (FileName, bytes) siap dilampirkan ke email.
        /// </summary>
        Task<List<AttachmentFile>> BuildAsync(
            TMSBilling.Models.Integration integration,
            Dictionary<string, object?> eventData);
    }

    public class AttachmentBuilder : IAttachmentBuilder
    {
        private readonly IQueryExecutor _queryExecutor;
        private readonly ILogger<AttachmentBuilder> _logger;

        public AttachmentBuilder(IQueryExecutor queryExecutor, ILogger<AttachmentBuilder> logger)
        {
            _queryExecutor = queryExecutor;
            _logger = logger;
        }

        public async Task<List<AttachmentFile>> BuildAsync(
            TMSBilling.Models.Integration integration,
            Dictionary<string, object?> eventData)
        {
            var activeAttachments = integration.Attachments
                .Where(a => a.IsActive)
                .OrderBy(a => a.GroupFileKey)
                .ThenBy(a => a.SheetOrder)
                .ToList();

            if (!activeAttachments.Any())
                return new List<AttachmentFile>();

            var result = new List<AttachmentFile>();

            // ── Group by GroupFileKey ─────────────────────────────────────────
            var groups = activeAttachments
                .GroupBy(a => a.GroupFileKey)
                .ToList();

            foreach (var group in groups)
            {
                var items = group.OrderBy(a => a.SheetOrder).ToList();
                var first = items.First();

                // Resolve file name dari attachment pertama dalam group
                var rawFileName = !string.IsNullOrWhiteSpace(first.GroupFileName)
                    ? first.GroupFileName
                    : $"{group.Key}_{{{{date}}}}.{first.FileFormat}";

                var fileName = PlaceholderHelper.Resolve(rawFileName, eventData);
                var format = first.FileFormat.ToLower();

                try
                {
                    AttachmentFile file;

                    // ── file_path type ────────────────────────────────────────
                    if (items.All(i => i.AttachmentType == "file_path"))
                    {
                        file = await BuildFromFilePaths(items, fileName, eventData);
                    }
                    // ── xlsx multi-sheet (query type, format=xlsx) ────────────
                    else if (format == "xlsx")
                    {
                        file = await BuildExcelMultiSheet(items, fileName, eventData);
                    }
                    // ── single format (csv / json / txt) — only uses first sheet ─
                    else
                    {
                        var rows = await ExecuteQueryForAttachment(first, eventData);
                        var bytes = format switch
                        {
                            "csv"  => GenerateCsv(rows),
                            "json" => GenerateJson(rows),
                            "txt"  => GenerateTxt(rows),
                            _      => GenerateCsv(rows)
                        };
                        file = new AttachmentFile
                        {
                            FileName = fileName,
                            Content = bytes,
                            ContentType = GetContentType(format)
                        };
                    }

                    result.Add(file);
                    _logger.LogInformation("Attachment built: {File} ({Size} bytes)", fileName, file.Content.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gagal build attachment group '{Key}'", group.Key);
                    // Lanjut ke group berikutnya, jangan batal semua
                }
            }

            return result;
        }

        // ── Excel Multi-Sheet ─────────────────────────────────────────────────

        private async Task<AttachmentFile> BuildExcelMultiSheet(
            List<IntegrationAttachment> items,
            string fileName,
            Dictionary<string, object?> eventData)
        {
            using var wb = new XLWorkbook();

            foreach (var item in items)
            {
                List<Dictionary<string, object?>> rows;

                if (item.AttachmentType == "file_path")
                {
                    // File path dalam multi-sheet tidak masuk akal untuk Excel,
                    // skip dengan warning
                    _logger.LogWarning("Attachment file_path diabaikan dalam multi-sheet Excel group.");
                    continue;
                }

                rows = await ExecuteQueryForAttachment(item, eventData);

                var sheetName = !string.IsNullOrWhiteSpace(item.SheetName)
                    ? SanitizeSheetName(item.SheetName)
                    : $"Sheet{item.SheetOrder}";

                WriteExcelSheet(wb, sheetName, rows);
            }

            // Pastikan ada minimal 1 sheet
            if (!wb.Worksheets.Any())
                wb.AddWorksheet("Data");

            using var ms = new MemoryStream();
            wb.SaveAs(ms);

            return new AttachmentFile
            {
                FileName = EnsureExtension(fileName, "xlsx"),
                Content = ms.ToArray(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        // ── File Path Attachment ──────────────────────────────────────────────

        private async Task<AttachmentFile> BuildFromFilePaths(
            List<IntegrationAttachment> items,
            string fileName,
            Dictionary<string, object?> eventData)
        {
            var first = items.First();
            var resolvedPath = PlaceholderHelper.Resolve(first.FilePath ?? string.Empty, eventData);

            if (!File.Exists(resolvedPath))
                throw new FileNotFoundException($"File attachment tidak ditemukan: {resolvedPath}");

            var bytes = await File.ReadAllBytesAsync(resolvedPath);
            var ext = Path.GetExtension(resolvedPath).TrimStart('.');

            return new AttachmentFile
            {
                FileName = string.IsNullOrWhiteSpace(fileName) ? Path.GetFileName(resolvedPath) : fileName,
                Content = bytes,
                ContentType = GetContentType(ext)
            };
        }

        // ── Excel Sheet Writer ────────────────────────────────────────────────

        private static void WriteExcelSheet(
            XLWorkbook wb,
            string sheetName,
            List<Dictionary<string, object?>> rows)
        {
            var ws = wb.AddWorksheet(sheetName);

            if (!rows.Any())
            {
                ws.Cell(1, 1).Value = "(tidak ada data)";
                return;
            }

            // Header — nama kolom persis dari SELECT alias
            var headers = rows[0].Keys.ToList();

            for (int col = 0; col < headers.Count; col++)
            {
                var cell = ws.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            // Data rows — preserve type untuk Excel rendering
            for (int rowIdx = 0; rowIdx < rows.Count; rowIdx++)
            {
                var rowBg = rowIdx % 2 == 0 ? XLColor.White : XLColor.FromHtml("#f8fafc");

                for (int col = 0; col < headers.Count; col++)
                {
                    var cell = ws.Cell(rowIdx + 2, col + 1);
                    cell.Style.Fill.BackgroundColor = rowBg;

                    var val = rows[rowIdx].GetValueOrDefault(headers[col]);

                    if (val == null)
                    {
                        cell.Value = string.Empty;
                    }
                    else if (val is bool b)
                    {
                        cell.Value = b;
                    }
                    else if (IsNumeric(val))
                    {
                        cell.Value = Convert.ToDouble(val);
                    }
                    else
                    {
                        // String (termasuk datetime yang sudah dikonversi ke string)
                        cell.Value = val.ToString();
                    }
                }
            }

            // Auto-fit columns
            ws.Columns().AdjustToContents(1, 50); // max 50 char width
        }

        // ── CSV / JSON / TXT generators ───────────────────────────────────────

        private static byte[] GenerateCsv(List<Dictionary<string, object?>> rows)
        {
            if (!rows.Any()) return Encoding.UTF8.GetBytes(string.Empty);

            var sb = new StringBuilder();
            var headers = rows[0].Keys.ToList();

            sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
            foreach (var row in rows)
                sb.AppendLine(string.Join(",", headers.Select(h => EscapeCsv(row.GetValueOrDefault(h)?.ToString()))));

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            // BOM prefix supaya Excel bisa buka CSV dengan karakter Indonesia
        }

        private static byte[] GenerateJson(List<Dictionary<string, object?>> rows)
        {
            var json = JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
            return Encoding.UTF8.GetBytes(json);
        }

        private static byte[] GenerateTxt(List<Dictionary<string, object?>> rows)
        {
            if (!rows.Any()) return Array.Empty<byte>();

            var sb = new StringBuilder();
            var headers = rows[0].Keys.ToList();
            var widths = headers.Select((h, i) =>
                Math.Max(h.Length, rows.Max(r => r.GetValueOrDefault(h)?.ToString()?.Length ?? 0))
            ).ToList();

            sb.AppendLine(string.Join(" | ", headers.Select((h, i) => h.PadRight(widths[i]))));
            sb.AppendLine(string.Join("-+-", widths.Select(w => new string('-', w))));
            foreach (var row in rows)
                sb.AppendLine(string.Join(" | ", headers.Select((h, i) =>
                    (row.GetValueOrDefault(h)?.ToString() ?? string.Empty).PadRight(widths[i]))));

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        // ── Query Executor helper ─────────────────────────────────────────────

        private async Task<List<Dictionary<string, object?>>> ExecuteQueryForAttachment(
            IntegrationAttachment attachment,
            Dictionary<string, object?> eventData)
        {
            if (string.IsNullOrWhiteSpace(attachment.Query))
                return new List<Dictionary<string, object?>>();

            _logger.LogInformation("Running attachment query for '{Name}'", attachment.DisplayName ?? attachment.SheetName);
            return await _queryExecutor.ExecuteAsync(attachment.Query, eventData);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsNumeric(object val) =>
            val is int or long or short or decimal or double or float;

        private static string EscapeCsv(string? value)
        {
            if (value == null) return string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        private static string SanitizeSheetName(string name)
        {
            // Excel sheet name: max 31 chars, no special chars
            var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
            var sanitized = string.Concat(name.Where(c => !invalid.Contains(c)));
            return sanitized.Length > 31 ? sanitized.Substring(0, 31) : sanitized;
        }

        private static string EnsureExtension(string fileName, string ext)
        {
            if (!fileName.EndsWith($".{ext}", StringComparison.OrdinalIgnoreCase))
                return $"{Path.GetFileNameWithoutExtension(fileName)}.{ext}";
            return fileName;
        }

        private static string GetContentType(string ext) => ext.ToLower() switch
        {
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "csv"  => "text/csv",
            "json" => "application/json",
            "txt"  => "text/plain",
            "pdf"  => "application/pdf",
            "zip"  => "application/zip",
            _      => "application/octet-stream"
        };
    }
}
