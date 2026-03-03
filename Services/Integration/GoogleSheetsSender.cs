using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;
using TMSBilling.Models;

namespace TMSBilling.Services.Integration
{
    public class GoogleSheetsSender : IChannelSender
    {
        private readonly ILogger<GoogleSheetsSender> _logger;

        public string ChannelType => "google_sheets";

        // Common column names used as auto-detected key columns (case-insensitive)
        private static readonly string[] AutoKeyColumns =
        {
            "SPK NO", "SPK NUMBER", "ORDER NO", "ORDER NUMBER", "NO ORDER",
            "SHIPMENT NO", "SHIPMENT NUMBER", "DO NO", "DO NUMBER",
            "INVOICE NO", "INVOICE NUMBER", "AWB", "AWB NO"
        };

        public GoogleSheetsSender(ILogger<GoogleSheetsSender> logger)
        {
            _logger = logger;
        }

        public async Task<SenderResult> SendAsync(SenderContext context)
        {
            var sw = Stopwatch.StartNew();
            var conn = context.Connection;

            if (string.IsNullOrWhiteSpace(conn.SpreadsheetId))
                return SenderResult.Fail("SpreadsheetId tidak dikonfigurasi.");

            if (string.IsNullOrWhiteSpace(conn.CredentialsJson))
                return SenderResult.Fail("CredentialsJson tidak dikonfigurasi.");

            try
            {
                var service = CreateSheetsService(conn.CredentialsJson);
                var sheetName = conn.SheetName ?? "Sheet1";
                var spreadsheetId = conn.SpreadsheetId;

                // Determine mode
                bool isScheduled = context.IsScheduled ||
                    (context.EventData.TryGetValue("scheduled", out var s) && s?.ToString() == "true");

                string mode;
                if (isScheduled && !string.IsNullOrWhiteSpace(conn.ScheduleMode))
                    mode = conn.ScheduleMode!;
                else
                    mode = "upsert"; // default for realtime

                _logger.LogInformation("GoogleSheets [{Sheet}] mode={Mode} rows={Count}",
                    sheetName, mode, context.Rows.Count);

                int rowCount;

                switch (mode.ToLower())
                {
                    case "overwrite":
                        rowCount = await HandleOverwriteAsync(service, spreadsheetId, sheetName, context.Rows);
                        break;
                    case "append":
                        rowCount = await HandleAppendAsync(service, spreadsheetId, sheetName, context.Rows);
                        break;
                    case "new_sheet":
                        sheetName = DateTime.Now.ToString("yyyy-MM-dd");
                        rowCount = await HandleNewSheetAsync(service, spreadsheetId, sheetName, context.Rows);
                        break;
                    case "upsert":
                    default:
                        rowCount = await HandleUpsertAsync(service, spreadsheetId, sheetName, conn, context.Rows);
                        break;
                }

                sw.Stop();
                return new SenderResult
                {
                    Success = true,
                    Message = $"Google Sheets [{sheetName}] {mode} selesai. {rowCount} baris diproses.",
                    RowCount = rowCount,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "GoogleSheetsSender error: {Msg}", ex.Message);
                return new SenderResult
                {
                    Success = false,
                    Message = "Gagal mengirim ke Google Sheets.",
                    ErrorDetail = ex.ToString(),
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
        }

        // ── Mode: Overwrite ───────────────────────────────────────────────────

        private async Task<int> HandleOverwriteAsync(SheetsService service,
            string spreadsheetId, string sheetName,
            List<Dictionary<string, object?>> rows)
        {
            await ClearSheetAsync(service, spreadsheetId, sheetName);
            if (rows.Count == 0) return 0;

            var headers = rows[0].Keys.ToList();
            var values = BuildValueRange(headers, rows);

            await service.Spreadsheets.Values.Update(values,
                spreadsheetId, $"{sheetName}!A1")
                .ExecuteWithRawValueInputOption();

            return rows.Count;
        }

        // ── Mode: Append ──────────────────────────────────────────────────────

        private async Task<int> HandleAppendAsync(SheetsService service,
            string spreadsheetId, string sheetName,
            List<Dictionary<string, object?>> rows)
        {
            if (rows.Count == 0) return 0;

            var headers = rows[0].Keys.ToList();
            var data = new List<IList<object?>>();
            foreach (var row in rows)
                data.Add(headers.Select(h => row.GetValueOrDefault(h)).ToList<object?>());

            var valueRange = new ValueRange { Values = data };
            var req = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, $"{sheetName}!A1");
            req.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            req.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
            await req.ExecuteAsync();

            return rows.Count;
        }

        // ── Mode: New Sheet (tab per date) ────────────────────────────────────

        private async Task<int> HandleNewSheetAsync(SheetsService service,
            string spreadsheetId, string sheetName,
            List<Dictionary<string, object?>> rows)
        {
            // Create sheet tab if not exists
            await EnsureSheetExistsAsync(service, spreadsheetId, sheetName);
            return await HandleOverwriteAsync(service, spreadsheetId, sheetName, rows);
        }

        // ── Mode: Upsert ──────────────────────────────────────────────────────

        private async Task<int> HandleUpsertAsync(SheetsService service,
            string spreadsheetId, string sheetName,
            IntegrationConnection conn,
            List<Dictionary<string, object?>> rows)
        {
            if (rows.Count == 0) return 0;

            // 1. Read existing sheet data
            var getReq = service.Spreadsheets.Values.Get(spreadsheetId, $"{sheetName}!A1:ZZ");
            getReq.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE;
            var sheetData = await getReq.ExecuteAsync();

            var existingRows = sheetData.Values ?? new List<IList<object?>>();

            // 2. Detect header row
            int headerRowIndex = (conn.HeaderRow ?? 1) - 1; // 0-based
            List<string> headers;

            if (existingRows.Count > headerRowIndex)
            {
                headers = existingRows[headerRowIndex]
                    .Select(c => c?.ToString() ?? string.Empty)
                    .ToList();
            }
            else
            {
                // Sheet empty — write headers from event data
                headers = rows[0].Keys.ToList();
                var valueRange = BuildValueRange(headers, rows);
                await service.Spreadsheets.Values.Update(valueRange, spreadsheetId, $"{sheetName}!A1")
                    .ExecuteWithRawValueInputOption();
                return rows.Count;
            }

            // 3. Resolve key column index
            int keyColIndex = ResolveKeyColumnIndex(headers, conn.KeyColumn);
            string keyColName = headers[keyColIndex];

            _logger.LogInformation("Upsert: KeyColumn='{Key}' (index {Idx})", keyColName, keyColIndex);

            // 4. Build map: keyValue → sheet row index (1-based, skipping header)
            int dataStartRow = headerRowIndex + 2; // 1-based for API
            var keyToSheetRow = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = headerRowIndex + 1; i < existingRows.Count; i++)
            {
                var sheetRow = existingRows[i];
                var keyVal = keyColIndex < sheetRow.Count
                    ? sheetRow[keyColIndex]?.ToString() ?? string.Empty
                    : string.Empty;

                if (!string.IsNullOrWhiteSpace(keyVal))
                    keyToSheetRow[keyVal] = i + 1; // 1-based sheet row
            }

            // 5. Process each incoming row
            var batchData = new BatchUpdateValuesRequest
            {
                ValueInputOption = "RAW",
                Data = new List<ValueRange>()
            };

            var rowsToAppend = new List<Dictionary<string, object?>>();

            foreach (var incomingRow in rows)
            {
                // Get key value from incoming data (case-insensitive key lookup)
                var incomingKeyVal = GetValueCaseInsensitive(incomingRow, keyColName)?.ToString() ?? string.Empty;

                if (keyToSheetRow.TryGetValue(incomingKeyVal, out int existingSheetRow))
                {
                    // UPDATE: build cell values aligned to existing headers
                    var rowValues = headers.Select(h => GetValueCaseInsensitive(incomingRow, h) as object ?? string.Empty).ToList();
                    batchData.Data.Add(new ValueRange
                    {
                        Range = $"{sheetName}!A{existingSheetRow}",
                        Values = new List<IList<object?>> { rowValues! }
                    });
                }
                else
                {
                    // INSERT: will append later
                    rowsToAppend.Add(incomingRow);
                }
            }

            // 6. Batch update existing rows
            if (batchData.Data.Count > 0)
            {
                await service.Spreadsheets.Values
                    .BatchUpdate(batchData, spreadsheetId)
                    .ExecuteAsync();
            }

            // 7. Append new rows
            if (rowsToAppend.Count > 0)
            {
                var appendData = new List<IList<object?>>();
                foreach (var row in rowsToAppend)
                {
                    appendData.Add(headers.Select(h => GetValueCaseInsensitive(row, h) as object ?? string.Empty).ToList<object?>());
                }

                var appendRange = new ValueRange { Values = appendData };
                var appendReq = service.Spreadsheets.Values.Append(appendRange, spreadsheetId, $"{sheetName}!A1");
                appendReq.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                appendReq.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
                await appendReq.ExecuteAsync();
            }

            return rows.Count;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private SheetsService CreateSheetsService(string credentialsJson)
        {
            var credential = GoogleCredential
                .FromJson(credentialsJson)
                .CreateScoped(SheetsService.Scope.Spreadsheets);

            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "TMSBilling Integration Hub"
            });
        }

        /// <summary>
        /// Resolve key column index with 3-tier priority:
        /// 1. KeyColumn from config (by name, case-insensitive)
        /// 2. Auto-detect from common column names
        /// 3. Fallback to column A (index 0)
        /// </summary>
        private int ResolveKeyColumnIndex(List<string> headers, string? configKeyColumn)
        {
            // Priority 1: Config KeyColumn
            if (!string.IsNullOrWhiteSpace(configKeyColumn))
            {
                int idx = headers.FindIndex(h => h.Equals(configKeyColumn, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) return idx;
                _logger.LogWarning("KeyColumn '{Key}' tidak ditemukan di header. Mencoba auto-detect.", configKeyColumn);
            }

            // Priority 2: Auto-detect
            foreach (var candidate in AutoKeyColumns)
            {
                int idx = headers.FindIndex(h => h.Equals(candidate, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    _logger.LogInformation("Auto-detect key column: '{Key}'", headers[idx]);
                    return idx;
                }
            }

            // Priority 3: Fallback to column A
            _logger.LogWarning("Key column tidak ditemukan. Menggunakan kolom A sebagai fallback.");
            return 0;
        }

        private ValueRange BuildValueRange(List<string> headers, List<Dictionary<string, object?>> rows)
        {
            var values = new List<IList<object?>>();
            values.Add(headers.Cast<object?>().ToList());
            foreach (var row in rows)
                values.Add(headers.Select(h => row.GetValueOrDefault(h) as object ?? string.Empty).ToList<object?>());

            return new ValueRange { Values = values };
        }

        private async Task ClearSheetAsync(SheetsService service, string spreadsheetId, string sheetName)
        {
            await service.Spreadsheets.Values
                .Clear(new ClearValuesRequest(), spreadsheetId, $"{sheetName}!A1:ZZ")
                .ExecuteAsync();
        }

        private async Task EnsureSheetExistsAsync(SheetsService service, string spreadsheetId, string sheetName)
        {
            var spreadsheet = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
            bool exists = spreadsheet.Sheets.Any(s =>
                s.Properties.Title.Equals(sheetName, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                var addReq = new BatchUpdateSpreadsheetRequest
                {
                    Requests = new List<Request>
                    {
                        new Request
                        {
                            AddSheet = new AddSheetRequest
                            {
                                Properties = new SheetProperties { Title = sheetName }
                            }
                        }
                    }
                };
                await service.Spreadsheets.BatchUpdate(addReq, spreadsheetId).ExecuteAsync();
            }
        }

        private object? GetValueCaseInsensitive(Dictionary<string, object?> row, string key)
        {
            var match = row.Keys.FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
            return match != null ? row[match] : null;
        }
    }

    // ── Extension method untuk ValueInputOption ───────────────────────────────
    internal static class SheetsExtensions
    {
        public static async Task<UpdateValuesResponse> ExecuteWithRawValueInputOption(
            this SpreadsheetsResource.ValuesResource.UpdateRequest req)
        {
            req.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            return await req.ExecuteAsync();
        }
    }
}
