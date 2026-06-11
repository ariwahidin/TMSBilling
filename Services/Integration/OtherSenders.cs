using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TMSBilling.Services.Integration
{
    // ──────────────────────────────────────────────────────────────────────────
    // FTP Sender
    // ──────────────────────────────────────────────────────────────────────────
    public class FtpSender : IChannelSender
    {
        private readonly ILogger<FtpSender> _logger;
        private readonly IFileGenerator _fileGenerator;
        private readonly IEncryptionService _encryption;

        public string ChannelType => "ftp";

        public FtpSender(ILogger<FtpSender> logger, IFileGenerator fileGenerator, IEncryptionService encryption)
        {
            _logger = logger;
            _fileGenerator = fileGenerator;
            _encryption = encryption;
        }

        public async Task<SenderResult> SendAsync(SenderContext context)
        {
            var sw = Stopwatch.StartNew();
            var conn = context.Connection;

            if (string.IsNullOrWhiteSpace(conn.Host))
                return SenderResult.Fail("FTP Host tidak dikonfigurasi.");

            try
            {
                var fileBytes = await _fileGenerator.GenerateAsync(context);
                var fileName = PlaceholderHelper.Resolve(conn.FileNameTemplate ?? "export_{{date}}.csv", context.EventData);
                var remotePath = (conn.RemotePath ?? "/").TrimEnd('/') + "/" + fileName;
                var password = string.IsNullOrWhiteSpace(conn.Password) ? null : _encryption.Decrypt(conn.Password);

                var ftpUrl = $"ftp://{conn.Host}:{conn.Port ?? 21}{remotePath}";
                var request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(conn.Username, password);
                request.ContentLength = fileBytes.Length;

                using var reqStream = await request.GetRequestStreamAsync();
                await reqStream.WriteAsync(fileBytes);

                using var response = (FtpWebResponse)await request.GetResponseAsync();
                _logger.LogInformation("FTP upload sukses: {Status}", response.StatusDescription);

                sw.Stop();
                return new SenderResult
                {
                    Success = true,
                    Message = $"File '{fileName}' berhasil diupload ke FTP: {remotePath}",
                    RowCount = context.Rows.Count,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new SenderResult { Success = false, Message = "Gagal upload via FTP.", ErrorDetail = ex.ToString(), DurationMs = sw.ElapsedMilliseconds };
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // API Sender
    // ──────────────────────────────────────────────────────────────────────────
    public class ApiSender : IChannelSender
    {
        private readonly ILogger<ApiSender> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEncryptionService _encryption;

        public string ChannelType => "api";

        public ApiSender(ILogger<ApiSender> logger, IHttpClientFactory httpClientFactory, IEncryptionService encryption)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _encryption = encryption;
        }

        public async Task<SenderResult> SendAsync(SenderContext context)
        {
            var sw = Stopwatch.StartNew();
            var conn = context.Connection;

            if (string.IsNullOrWhiteSpace(conn.ApiUrl))
                return SenderResult.Fail("API URL tidak dikonfigurasi.");

            try
            {
                var client = _httpClientFactory.CreateClient("IntegrationHub");

                // Set token if available



                //if (!string.IsNullOrWhiteSpace(conn.ApiToken))
                //{
                //    var decryptedToken = _encryption.Decrypt(conn.ApiToken);
                //    client.DefaultRequestHeaders.Authorization =
                //        new AuthenticationHeaderValue("Bearer", decryptedToken);
                //}

                // Set custom headers
                //if (!string.IsNullOrWhiteSpace(conn.ApiHeaders))
                //{
                //    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(conn.ApiHeaders);
                //    if (headers != null)
                //        foreach (var (k, v) in headers)
                //            client.DefaultRequestHeaders.TryAddWithoutValidation(k, v);
                //}

                // Set custom headers DULU
                if (!string.IsNullOrWhiteSpace(conn.ApiHeaders))
                {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(conn.ApiHeaders);
                    if (headers != null)
                        foreach (var (k, v) in headers)
                        {
                            _logger.LogDebug("Header: {Key} = {Value}", k, v); // ← log
                            client.DefaultRequestHeaders.TryAddWithoutValidation(k, v);
                        }
                }

                // Set ApiToken HANYA jika tidak ada Authorization di custom headers
                if (!string.IsNullOrWhiteSpace(conn.ApiToken) &&
                    !client.DefaultRequestHeaders.Contains("Authorization"))
                {
                    var decryptedToken = _encryption.Decrypt(conn.ApiToken);
                    _logger.LogDebug("Using ApiToken as Bearer");
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", decryptedToken);
                }

                // Log final Authorization
                _logger.LogDebug("Final Authorization: {Auth}",
                    client.DefaultRequestHeaders.Authorization?.ToString());

                // Build payload: array of rows OR single event data
                //object payload = context.Rows.Count > 0 ? (object)context.Rows : context.EventData;
                //var json = JsonSerializer.Serialize(payload);

                //string json;
                //if (!string.IsNullOrWhiteSpace(conn.PayloadTemplate))
                //{
                //    // Render template dengan data dari row pertama (atau eventData)
                //    var data = context.Rows.Count > 0 ? context.Rows[0] : context.EventData;
                //    json = ResolvePlaceholders(conn.PayloadTemplate, data);

                //    _logger.LogDebug(">> Using PayloadTemplate, resolved length: {Len}", json.Length);
                //}
                //else
                //{
                //    object payload = context.Rows.Count > 0 ? (object)context.Rows : context.EventData;
                //    json = JsonSerializer.Serialize(payload);
                //}

                string json;
                if (!string.IsNullOrWhiteSpace(conn.PayloadTemplate))
                {
                    if (conn.PayloadTemplate.Contains("{{__rows__}}") && !string.IsNullOrWhiteSpace(conn.ItemTemplate))
                    {
                        var items = context.Rows
                            .Select(row => ResolvePlaceholders(conn.ItemTemplate, row))
                            .ToList();
                        var arrayJson = "[" + string.Join(",", items) + "]";
                        json = conn.PayloadTemplate.Replace("{{__rows__}}", arrayJson);
                    }
                    else
                    {
                        var data = context.Rows.Count > 0 ? context.Rows[0] : context.EventData;
                        json = ResolvePlaceholders(conn.PayloadTemplate, data);
                    }
                    _logger.LogDebug(">> Using PayloadTemplate, resolved length: {Len}", json.Length);
                }
                else
                {
                    object payload = context.Rows.Count > 0 ? (object)context.Rows : context.EventData;
                    json = JsonSerializer.Serialize(payload);
                }



                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var method = (conn.ApiMethod ?? "POST").ToUpper();

                // Log semua headers yang akan dikirim
                foreach (var h in client.DefaultRequestHeaders)
                    _logger.LogDebug(">> Header: {Key}: {Value}", h.Key, string.Join(", ", h.Value));

                _logger.LogDebug(">> URL: {Method} {Url}", method, conn.ApiUrl);

                _logger.LogDebug(">> Payload: {Json}", json);




                HttpResponseMessage response = method switch
                {
                    "GET"   => await client.GetAsync(conn.ApiUrl),
                    "PUT"   => await client.PutAsync(conn.ApiUrl, content),
                    "PATCH" => await client.PatchAsync(conn.ApiUrl, content),
                    _       => await client.PostAsync(conn.ApiUrl, content)
                };

                var responseBody = await response.Content.ReadAsStringAsync();
                //var responseBody = await response.Content.ReadAsStringAsync();

                sw.Stop();
                if (response.IsSuccessStatusCode)
                {
                    return new SenderResult
                    {
                        Success = true,
                        Message = $"API {method} {conn.ApiUrl} → {(int)response.StatusCode}",
                        RowCount = context.Rows.Count,
                        DurationMs = sw.ElapsedMilliseconds,
                        ResponseBody = responseBody,   // ← tambah
                        RequestPayload = json          // ← tambah
                    };
                }
                else
                {
                    return new SenderResult
                    {
                        Success = false,
                        Message = $"API response error: {(int)response.StatusCode}",
                        ErrorDetail = responseBody,
                        DurationMs = sw.ElapsedMilliseconds,
                        ResponseBody = responseBody,   // ← tambah
                        RequestPayload = json          // ← tambah
                    };
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new SenderResult { Success = false, Message = "API call gagal.", ErrorDetail = ex.ToString(), DurationMs = sw.ElapsedMilliseconds };
            }
        }

        private static string ResolvePlaceholders(string template, Dictionary<string, object?> data)
        {
            foreach (var (key, value) in data)
            {
                string strVal = value switch
                {
                    null => "null",
                    bool b => b.ToString().ToLower(),
                    int or long or float or double
                        or decimal => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "0",
                    _ => JsonSerializer.Serialize(value.ToString())[1..^1]
                };
                template = template.Replace($"{{{{{key}}}}}", strVal);
            }
            return template;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // File Sender
    // ──────────────────────────────────────────────────────────────────────────
    public class FileSender : IChannelSender
    {
        private readonly ILogger<FileSender> _logger;
        private readonly IFileGenerator _fileGenerator;

        public string ChannelType => "file";

        public FileSender(ILogger<FileSender> logger, IFileGenerator fileGenerator)
        {
            _logger = logger;
            _fileGenerator = fileGenerator;
        }

        public async Task<SenderResult> SendAsync(SenderContext context)
        {
            var sw = Stopwatch.StartNew();
            var conn = context.Connection;

            if (string.IsNullOrWhiteSpace(conn.FileOutputPath))
                return SenderResult.Fail("File output path tidak dikonfigurasi.");

            try
            {
                var fileBytes = await _fileGenerator.GenerateAsync(context);
                var fileName = PlaceholderHelper.Resolve(conn.FileNameTemplate ?? "export_{{date}}.csv", context.EventData);
                var outputDir = conn.FileOutputPath;

                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                var fullPath = Path.Combine(outputDir, fileName);
                await File.WriteAllBytesAsync(fullPath, fileBytes);

                sw.Stop();
                _logger.LogInformation("File saved: {Path}", fullPath);
                return new SenderResult
                {
                    Success = true,
                    Message = $"File disimpan: {fullPath}",
                    RowCount = context.Rows.Count,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new SenderResult { Success = false, Message = "Gagal menyimpan file.", ErrorDetail = ex.ToString(), DurationMs = sw.ElapsedMilliseconds };
            }
        }
    }

    

}
