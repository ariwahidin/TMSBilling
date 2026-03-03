//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace TMSBilling.Models
//{
//    [Table("Integrations")]
//    public class Integration
//    {
//        [Key]
//        public int Id { get; set; }

//        [Required, MaxLength(200)]
//        public string Name { get; set; } = string.Empty;

//        [Required, MaxLength(200)]
//        public string EventKey { get; set; } = string.Empty;

//        /// <summary>sftp | ftp | api | file | google_sheets</summary>
//        [Required, MaxLength(50)]
//        public string ChannelType { get; set; } = string.Empty;

//        /// <summary>query | event</summary>
//        [Required, MaxLength(50)]
//        public string SourceType { get; set; } = "event";

//        /// <summary>realtime | scheduled</summary>
//        [Required, MaxLength(50)]
//        public string Timing { get; set; } = "realtime";

//        /// <summary>SQL SELECT query (used when SourceType = query)</summary>
//        public string? Query { get; set; }

//        // ── Schedule Config ───────────────────────────────────────────────
//        /// <summary>daily | weekly | monthly</summary>
//        [MaxLength(50)]
//        public string? ScheduleFreq { get; set; }

//        public int? ScheduleHour { get; set; }
//        public int? ScheduleMinute { get; set; }

//        /// <summary>0=Sunday ... 6=Saturday (used when ScheduleFreq = weekly)</summary>
//        public int? ScheduleDayOfWeek { get; set; }

//        /// <summary>1-31 (used when ScheduleFreq = monthly)</summary>
//        public int? ScheduleDayOfMonth { get; set; }

//        // ── Email Notification Config ────────────────────────────────────
//        public bool SendEmailOnSuccess { get; set; } = false;
//        public bool SendEmailOnFailure { get; set; } = true;

//        [MaxLength(500)]
//        public string? EmailSubjectTemplate { get; set; }

//        public string? EmailBodyTemplate { get; set; }

//        // ── Status ───────────────────────────────────────────────────────
//        public bool IsActive { get; set; } = true;

//        public DateTime? LastRunAt { get; set; }
//        public DateTime? NextRunAt { get; set; }

//        [MaxLength(50)]
//        public string? LastRunStatus { get; set; }

//        public string? Description { get; set; }

//        public DateTime CreatedAt { get; set; } = DateTime.Now;
//        public DateTime UpdatedAt { get; set; } = DateTime.Now;

//        // ── Navigation ───────────────────────────────────────────────────
//        public IntegrationConnection? Connection { get; set; }
//        public ICollection<IntegrationRecipient> Recipients { get; set; } = new List<IntegrationRecipient>();
//        public ICollection<IntegrationHistory> Histories { get; set; } = new List<IntegrationHistory>();
//    }
//}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("Integrations")]
    public class Integration
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string EventKey { get; set; } = string.Empty;

        /// <summary>sftp | ftp | api | file | google_sheets</summary>
        [Required, MaxLength(50)]
        public string ChannelType { get; set; } = string.Empty;

        /// <summary>query | event</summary>
        [Required, MaxLength(50)]
        public string SourceType { get; set; } = "event";

        /// <summary>realtime | scheduled</summary>
        [Required, MaxLength(50)]
        public string Timing { get; set; } = "realtime";

        /// <summary>SQL SELECT query (used when SourceType = query)</summary>
        public string? Query { get; set; }

        // ── Schedule Config ───────────────────────────────────────────────
        /// <summary>daily | weekly | monthly</summary>
        [MaxLength(50)]
        public string? ScheduleFreq { get; set; }

        public int? ScheduleHour { get; set; }
        public int? ScheduleMinute { get; set; }

        /// <summary>0=Sunday ... 6=Saturday</summary>
        public int? ScheduleDayOfWeek { get; set; }

        /// <summary>1-31</summary>
        public int? ScheduleDayOfMonth { get; set; }

        // ── Email Notification Config ────────────────────────────────────
        public bool SendEmailOnSuccess { get; set; } = false;
        public bool SendEmailOnFailure { get; set; } = true;

        [MaxLength(500)]
        public string? EmailSubjectTemplate { get; set; }

        /// <summary>
        /// HTML template untuk body email.
        /// Support: {{field}}, {{__data_table__}}, {{__summary__}}, {{__error_box__}}
        /// Kosong = gunakan default template otomatis.
        /// </summary>
        public string? EmailBodyTemplate { get; set; }

        /// <summary>HTML template footer. Kosong = default footer.</summary>
        public string? EmailFooterTemplate { get; set; }

        /// <summary>
        /// Apakah sertakan inline data table di body email.
        /// </summary>
        public bool EmailIncludeDataTable { get; set; } = true;

        /// <summary>
        /// Mode attachment email:
        /// none       = tidak ada attachment
        /// inline     = data table di body saja
        /// attachment = file attachment saja (no inline table)
        /// both       = inline table + attachment
        /// </summary>
        [MaxLength(20)]
        public string EmailAttachmentMode { get; set; } = "inline";

        // ── Status ───────────────────────────────────────────────────────
        public bool IsActive { get; set; } = true;

        public DateTime? LastRunAt { get; set; }
        public DateTime? NextRunAt { get; set; }

        [MaxLength(50)]
        public string? LastRunStatus { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ── Navigation ───────────────────────────────────────────────────
        public IntegrationConnection? Connection { get; set; }
        public ICollection<IntegrationRecipient> Recipients { get; set; } = new List<IntegrationRecipient>();
        public ICollection<IntegrationHistory> Histories { get; set; } = new List<IntegrationHistory>();
        public ICollection<IntegrationAttachment> Attachments { get; set; } = new List<IntegrationAttachment>();
    }
}
