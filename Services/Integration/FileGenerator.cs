using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace TMSBilling.Services.Integration
{
    public interface IFileGenerator
    {
        Task<byte[]> GenerateAsync(SenderContext context);
    }

    public class FileGenerator : IFileGenerator
    {
        public async Task<byte[]> GenerateAsync(SenderContext context)
        {
            var format = (context.Connection.FileFormat ?? "csv").ToLower();

            return format switch
            {
                "xlsx" => await Task.Run(() => GenerateExcel(context.Rows)),
                "json" => await Task.Run(() => GenerateJson(context.Rows)),
                "txt"  => await Task.Run(() => GenerateTxt(context.Rows)),
                _      => await Task.Run(() => GenerateCsv(context.Rows))  // default csv
            };
        }

        private byte[] GenerateCsv(List<Dictionary<string, object?>> rows)
        {
            if (rows.Count == 0) return Encoding.UTF8.GetBytes(string.Empty);

            var sb = new StringBuilder();
            var headers = rows[0].Keys.ToList();

            // Header
            sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

            // Rows
            foreach (var row in rows)
                sb.AppendLine(string.Join(",", headers.Select(h => EscapeCsv(row.GetValueOrDefault(h)?.ToString()))));

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private byte[] GenerateExcel(List<Dictionary<string, object?>> rows)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Data");

            if (rows.Count == 0)
            {
                using var ms0 = new MemoryStream();
                wb.SaveAs(ms0);
                return ms0.ToArray();
            }

            var headers = rows[0].Keys.ToList();

            // Header row
            for (int col = 0; col < headers.Count; col++)
            {
                var cell = ws.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            // Data rows
            for (int row = 0; row < rows.Count; row++)
            {
                for (int col = 0; col < headers.Count; col++)
                {
                    var val = rows[row].GetValueOrDefault(headers[col]);
                    var cell = ws.Cell(row + 2, col + 1);

                    // Preserve types
                    if (val is DateTime dt)
                        cell.Value = dt;
                    else if (val is bool b)
                        cell.Value = b;
                    else if (val is double or float or int or long or decimal)
                        cell.Value = Convert.ToDouble(val);
                    else
                        cell.Value = val?.ToString() ?? string.Empty;
                }
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private byte[] GenerateJson(List<Dictionary<string, object?>> rows)
        {
            var json = JsonSerializer.Serialize(rows, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            return Encoding.UTF8.GetBytes(json);
        }

        private byte[] GenerateTxt(List<Dictionary<string, object?>> rows)
        {
            if (rows.Count == 0) return Array.Empty<byte>();

            var sb = new StringBuilder();
            var headers = rows[0].Keys.ToList();

            // Fixed-width calculation
            var widths = headers.Select((h, i) =>
                Math.Max(h.Length, rows.Max(r => r.GetValueOrDefault(h)?.ToString()?.Length ?? 0))
            ).ToList();

            // Header
            sb.AppendLine(string.Join(" | ", headers.Select((h, i) => h.PadRight(widths[i]))));
            sb.AppendLine(string.Join("-+-", widths.Select(w => new string('-', w))));

            // Rows
            foreach (var row in rows)
                sb.AppendLine(string.Join(" | ", headers.Select((h, i) =>
                    (row.GetValueOrDefault(h)?.ToString() ?? string.Empty).PadRight(widths[i]))));

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private string EscapeCsv(string? value)
        {
            if (value == null) return string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
