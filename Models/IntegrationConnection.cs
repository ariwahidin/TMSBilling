using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    /// <summary>
    /// Stores channel-specific credentials and config per integration.
    /// One-to-one with Integration.
    /// </summary>
    [Table("IntegrationConnections")]
    public class IntegrationConnection
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Integration))]
        public int IntegrationId { get; set; }
        public Integration? Integration { get; set; }

        // ── SFTP / FTP ────────────────────────────────────────────────────
        [MaxLength(500)]
        public string? Host { get; set; }

        public int? Port { get; set; }

        [MaxLength(200)]
        public string? Username { get; set; }

        /// <summary>Encrypted password</summary>
        [MaxLength(1000)]
        public string? Password { get; set; }

        /// <summary>Remote directory path</summary>
        [MaxLength(500)]
        public string? RemotePath { get; set; }

        /// <summary>Private key content (SFTP key auth)</summary>
        public string? PrivateKey { get; set; }

        // ── API ───────────────────────────────────────────────────────────
        [MaxLength(2000)]
        public string? ApiUrl { get; set; }

        /// <summary>GET | POST | PUT | PATCH</summary>
        [MaxLength(10)]
        public string? ApiMethod { get; set; }

        /// <summary>JSON string of headers</summary>
        public string? ApiHeaders { get; set; }

        /// <summary>Bearer token or API Key (encrypted)</summary>
        [MaxLength(2000)]
        public string? ApiToken { get; set; }

        /// <summary>
        /// Optional JSON template with {{placeholder}} from query columns.
        /// If set, overrides default flat-array payload.
        /// </summary>
        public string? PayloadTemplate { get; set; }
        public string? ItemTemplate { get; set; }

        // ── File ──────────────────────────────────────────────────────────
        [MaxLength(1000)]
        public string? FileOutputPath { get; set; }

        /// <summary>Filename template e.g. "ORDER_{{date}}_{{order_no}}.csv"</summary>
        [MaxLength(500)]
        public string? FileNameTemplate { get; set; }

        /// <summary>csv | xlsx | json | txt</summary>
        [MaxLength(20)]
        public string? FileFormat { get; set; }

        // ── Google Sheets ─────────────────────────────────────────────────
        [MaxLength(200)]
        public string? SpreadsheetId { get; set; }

        [MaxLength(200)]
        public string? SheetName { get; set; }

        /// <summary>Service account credentials JSON (stored as-is in DB)</summary>
        public string? CredentialsJson { get; set; }

        /// <summary>Row number of header (usually 1)</summary>
        public int? HeaderRow { get; set; } = 1;

        /// <summary>Column name used as unique key for upsert</summary>
        [MaxLength(100)]
        public string? KeyColumn { get; set; }

        /// <summary>upsert | overwrite | append | new_sheet</summary>
        [MaxLength(50)]
        public string? ScheduleMode { get; set; } = "upsert";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
