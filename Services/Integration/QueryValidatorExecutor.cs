using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TMSBilling.Services.Integration
{
    public interface IQueryValidator
    {
        (bool IsValid, string? Error) Validate(string query);
    }

    public interface IQueryExecutor
    {
        Task<List<Dictionary<string, object?>>> ExecuteAsync(
            string query, Dictionary<string, object?> parameters, int maxRows = 10000);

        Task<(List<Dictionary<string, object?>> Rows, string? Error)> PreviewAsync(
            string query, int maxRows = 100);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // QueryValidator
    // ──────────────────────────────────────────────────────────────────────────
    public class QueryValidator : IQueryValidator
    {
        // Keyword yang DILARANG (DML / DDL / dangerous)
        private static readonly string[] ForbiddenPatterns =
        {
            @"\bINSERT\b", @"\bUPDATE\b", @"\bDELETE\b", @"\bDROP\b",
            @"\bTRUNCATE\b", @"\bALTER\b", @"\bCREATE\b", @"\bEXEC\b",
            @"\bEXECUTE\b", @"\bXP_\w+", @"\bMERGE\b", @"\bBULK\s+INSERT\b",
            @"\bOPENROWSET\b", @"\bOPENDATASOURCE\b",
            @"\bGRANT\b", @"\bREVOKE\b", @"\bDENY\b", @"\bSHUTDOWN\b",
            // Dangerous SPs
            @"\bSP_EXECUTESQL\b", @"\bSP_CONFIGURE\b"
        };

        // DECLARE dan SET diizinkan hanya dalam CTE context (tidak sebagai statement awal)
        // DECLARE sebagai statement standalone → dilarang
        private static readonly string[] StandaloneStatementPatterns =
        {
            @"^\s*DECLARE\b", @"^\s*SET\b"
        };

        public (bool IsValid, string? Error) Validate(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return (false, "Query tidak boleh kosong.");

            // Strip string literals sebelum validasi untuk hindari false positive
            var stripped = StripStringLiterals(query);
            var upper = stripped.ToUpperInvariant();

            // Harus diawali SELECT atau WITH (CTE)
            var trimmedUpper = upper.TrimStart();
            if (!trimmedUpper.StartsWith("SELECT") && !trimmedUpper.StartsWith("WITH"))
                return (false, "Query harus dimulai dengan SELECT atau WITH (CTE).");

            // Kalau WITH, harus diikuti CTE pattern: WITH name AS (
            if (trimmedUpper.StartsWith("WITH"))
            {
                // Pastikan ada SELECT setelah CTE definition
                if (!Regex.IsMatch(upper, @"\bSELECT\b", RegexOptions.IgnoreCase))
                    return (false, "Query WITH harus mengandung SELECT.");
            }

            // Check forbidden keywords
            foreach (var pattern in ForbiddenPatterns)
            {
                if (Regex.IsMatch(upper, pattern))
                {
                    var keyword = Regex.Match(upper, pattern).Value.Trim();
                    return (false, $"Query mengandung keyword yang tidak diizinkan: {keyword}");
                }
            }

            // DECLARE/SET sebagai statement standalone (di awal query) → dilarang
            foreach (var pattern in StandaloneStatementPatterns)
            {
                if (Regex.IsMatch(upper, pattern))
                    return (false, $"DECLARE/SET sebagai statement standalone tidak diizinkan. Gunakan CTE jika perlu variable.");
            }

            // Comment injection
            if (upper.Contains("--") || upper.Contains("/*"))
                return (false, "Query tidak boleh mengandung SQL comment (-- atau /* */).");

            // Semicolon = multiple statements
            if (upper.Contains(";"))
                return (false, "Query tidak boleh mengandung titik koma (;). Hanya satu statement yang diizinkan.");

            return (true, null);
        }

        /// <summary>
        /// Ganti isi string literal ('...') dengan placeholder kosong
        /// supaya keyword di dalam string tidak trigger validasi.
        /// Contoh: WHERE status = 'DELETE ME' → WHERE status = ''
        /// </summary>
        private static string StripStringLiterals(string sql)
        {
            return Regex.Replace(sql, @"'(?:[^']|'')*'", "''");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // QueryExecutor
    // ──────────────────────────────────────────────────────────────────────────
    public class QueryExecutor : IQueryExecutor
    {
        private readonly IConfiguration _config;
        private readonly ILogger<QueryExecutor> _logger;
        private readonly QueryValidator _validator;

        private const int QueryTimeoutSeconds = 60;

        public QueryExecutor(IConfiguration config, ILogger<QueryExecutor> logger)
        {
            _config = config;
            _logger = logger;
            _validator = new QueryValidator();
        }

        public async Task<List<Dictionary<string, object?>>> ExecuteAsync(
            string query, Dictionary<string, object?> parameters, int maxRows = 10000)
        {
            var (isValid, error) = _validator.Validate(query);
            if (!isValid)
                throw new InvalidOperationException($"Query tidak valid: {error}");

            return await RunAsync(query, parameters, maxRows);
        }

        public async Task<(List<Dictionary<string, object?>> Rows, string? Error)> PreviewAsync(
            string query, int maxRows = 100)
        {
            var (isValid, error) = _validator.Validate(query);
            if (!isValid)
                return (new(), error);

            try
            {
                var rows = await RunAsync(query, new(), maxRows);
                return (rows, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Query preview error");
                return (new(), ex.Message);
            }
        }

        private async Task<List<Dictionary<string, object?>>> RunAsync(
            string query, Dictionary<string, object?> parameters, int maxRows)
        {
            var connectionString = _config.GetConnectionString("DefaultConnection")!;
            var results = new List<Dictionary<string, object?>>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // ── Smart query wrapping ──────────────────────────────────────────
            // Inject TOP limit tanpa merusak CTE, UNION, ORDER BY, subquery.
            // Strategy: inject SET ROWCOUNT sebelum query (tidak mengubah struktur query).
            // Lebih aman dari wrapping SELECT TOP * FROM (...) karena tidak
            // mempengaruhi ORDER BY, CTE names, column aliases, dll.
            var finalQuery = $"SET ROWCOUNT {maxRows};\n{query.TrimEnd().TrimEnd(';')}";

            _logger.LogDebug("Executing query (maxRows={Max}):\n{Query}", maxRows, query);

            await using var command = new SqlCommand(finalQuery, connection)
            {
                CommandTimeout = QueryTimeoutSeconds
            };

            // Inject event data sebagai SQL parameters (safe from injection)
            // Placeholder {{field}} di query diganti dengan @field
            var resolvedQuery = ResolveQueryParameters(finalQuery, parameters, command);
            command.CommandText = resolvedQuery;

            await using var reader = await command.ExecuteReaderAsync();

            // Skip result sets dari SET ROWCOUNT (tidak ada result set, langsung ke data)
            do
            {
                if (!reader.HasRows) continue;

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        // Gunakan nama kolom persis seperti di SELECT (termasuk alias)
                        var colName = reader.GetName(i);

                        // Handle duplicate column names (edge case JOIN)
                        if (row.ContainsKey(colName))
                            colName = $"{colName}_{i}";

                        var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[colName] = ConvertValue(val);
                    }
                    results.Add(row);
                }

            } while (await reader.NextResultAsync() && results.Count == 0);
            // Hanya ambil result set pertama yang punya data

            _logger.LogDebug("Query returned {Count} rows", results.Count);
            return results;
        }

        /// <summary>
        /// Ganti {{field}} di query dengan @field SQL parameter.
        /// Contoh: WHERE order_no = '{{order_no}}' → WHERE order_no = @order_no
        /// </summary>
        private static string ResolveQueryParameters(
            string query,
            Dictionary<string, object?> parameters,
            SqlCommand command)
        {
            if (!parameters.Any()) return query;

            var result = query;
            foreach (var (key, value) in parameters)
            {
                var placeholder = $"{{{{{key}}}}}"; // {{key}}
                var paramName = $"@{Regex.Replace(key, @"[^a-zA-Z0-9_]", "_")}";

                if (result.Contains(placeholder))
                {
                    result = result.Replace(placeholder, paramName);
                    command.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                }
            }
            return result;
        }

        /// <summary>
        /// Konversi tipe data SQL ke tipe yang aman untuk serialisasi.
        /// DateTime dipertahankan sebagai string ISO agar kolom tanggal
        /// tidak diformat ulang oleh Google Sheets / Excel.
        /// </summary>
        private static object? ConvertValue(object? val)
        {
            return val switch
            {
                null          => null,
                DateTime dt   => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss"),
                byte[] _      => "[binary]",
                _             => val
            };
        }
    }
}
