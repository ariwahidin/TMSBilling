using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace TMSBilling.Services.Integration
{
    /// <summary>
    /// Requires NuGet: SSH.NET
    /// </summary>
    public class SftpSender : IChannelSender
    {
        private readonly ILogger<SftpSender> _logger;
        private readonly IFileGenerator _fileGenerator;
        private readonly IEncryptionService _encryption;

        public string ChannelType => "sftp";

        public SftpSender(ILogger<SftpSender> logger, IFileGenerator fileGenerator, IEncryptionService encryption)
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
                return SenderResult.Fail("SFTP Host tidak dikonfigurasi.");

            try
            {
                // Generate file content
                var fileBytes = await _fileGenerator.GenerateAsync(context);
                var fileName = PlaceholderHelper.Resolve(conn.FileNameTemplate ?? "export_{{date}}.csv", context.EventData);
                var remotePath = (conn.RemotePath ?? "/").TrimEnd('/') + "/" + fileName;

                var password = string.IsNullOrWhiteSpace(conn.Password) ? null : _encryption.Decrypt(conn.Password);

                AuthenticationMethod auth = !string.IsNullOrWhiteSpace(conn.PrivateKey)
                    ? new PrivateKeyAuthenticationMethod(conn.Username, new PrivateKeyFile(
                        new MemoryStream(System.Text.Encoding.UTF8.GetBytes(conn.PrivateKey))))
                    : new PasswordAuthenticationMethod(conn.Username, password);

                var connectionInfo = new Renci.SshNet.ConnectionInfo(
                    conn.Host, conn.Port ?? 22, conn.Username, auth);

                await Task.Run(() =>
                {
                    using var sftp = new SftpClient(connectionInfo);
                    sftp.Connect();
                    using var stream = new MemoryStream(fileBytes);
                    sftp.UploadFile(stream, remotePath, true);
                    sftp.Disconnect();
                });

                sw.Stop();
                _logger.LogInformation("SFTP upload sukses: {Path}", remotePath);
                return new SenderResult
                {
                    Success = true,
                    Message = $"File '{fileName}' berhasil diupload ke SFTP: {remotePath}",
                    RowCount = context.Rows.Count,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "SFTP error");
                return new SenderResult
                {
                    Success = false,
                    Message = "Gagal upload via SFTP.",
                    ErrorDetail = ex.ToString(),
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
        }
    }
}
