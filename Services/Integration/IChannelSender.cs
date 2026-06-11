using System.Collections.Generic;
using System.Threading.Tasks;

namespace TMSBilling.Services.Integration
{
    /// <summary>
    /// Context yang dikirim ke setiap sender.
    /// </summary>
    public class SenderContext
    {
        public TMSBilling.Models.Integration Integration { get; set; } = null!;
        public TMSBilling.Models.IntegrationConnection Connection { get; set; } = null!;

        /// <summary>Event data dari dispatcher (payload or query result)</summary>
        public List<Dictionary<string, object?>> Rows { get; set; } = new();

        /// <summary>Raw event data untuk placeholder resolution</summary>
        public Dictionary<string, object?> EventData { get; set; } = new();

        public bool IsScheduled { get; set; } = false;
    }

    public class SenderResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorDetail { get; set; }
        public string? ResponseBody { get; set; }   // ← tambah
        public string? RequestPayload { get; set; } // ← tambah
        public int RowCount { get; set; }
        public long DurationMs { get; set; }

        public static SenderResult Ok(string message, int rowCount = 0) =>
            new() { Success = true, Message = message, RowCount = rowCount };

        public static SenderResult Fail(string error, string? detail = null) =>
            new() { Success = false, Message = error, ErrorDetail = detail };
    }

    public interface IChannelSender
    {
        /// <summary>Channel type yang di-handle: sftp | ftp | api | file | google_sheets</summary>
        string ChannelType { get; }

        Task<SenderResult> SendAsync(SenderContext context);
    }
}
