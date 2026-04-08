using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TMSBilling.Models
{
    // ─────────────────────────────────────────────
    // 1. Definisi Report
    // ─────────────────────────────────────────────
    [Table("RPT_MAIL_REPORT")]
    public class MailReport
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required, StringLength(100)]
        public required string report_code { get; set; }       // e.g. "SPK_NOTICE"

        [Required, StringLength(200)]
        public required string report_name { get; set; }       // e.g. "Surat Perintah Kirim"

        [StringLength(500)]
        public string? report_desc { get; set; }

        // Trigger type: EVENT | SCHEDULED | MANUAL
        [Required, StringLength(20)]
        public required string trigger_type { get; set; }

        // Untuk EVENT: nama event key e.g. "order.created", "spk.approved"
        [StringLength(100)]
        public string? event_key { get; set; }

        // Untuk SCHEDULED
        [StringLength(20)]
        public string? schedule_freq { get; set; }             // DAILY | WEEKLY | MONTHLY

        public TimeSpan? schedule_time { get; set; }           // jam kirim
        public int? schedule_day_of_week { get; set; }         // 1=Mon .. 7=Sun (untuk WEEKLY)
        public int? schedule_day_of_month { get; set; }        // 1-28 (untuk MONTHLY)

        public byte is_active { get; set; } = 1;

        [StringLength(50)]
        public string? entry_user { get; set; }
        public DateTime? entry_date { get; set; }
        [StringLength(50)]
        public string? update_user { get; set; }
        public DateTime? update_date { get; set; }

        // Navigation
        [JsonIgnore] public MailReportTemplate? Template { get; set; }
        [JsonIgnore] public List<MailReportSection> Sections { get; set; } = new();
        [JsonIgnore] public List<MailReportRecipient> Recipients { get; set; } = new();
    }

    // ─────────────────────────────────────────────
    // 2. Template (Subject, Body, Footer)
    // ─────────────────────────────────────────────
    [Table("RPT_MAIL_TEMPLATE")]
    public class MailReportTemplate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int report_id { get; set; }

        [Required, StringLength(300)]
        public required string subject { get; set; }           // support placeholder {{field}}

        [Column(TypeName = "nvarchar(max)")]
        public string? body_header { get; set; }               // HTML atas tabel

        [Column(TypeName = "nvarchar(max)")]
        public string? body_footer { get; set; }               // HTML bawah tabel

        // Attachment settings
        public byte attach_pdf { get; set; } = 0;
        public byte attach_excel { get; set; } = 0;

        [StringLength(200)]
        public string? attach_filename { get; set; }           // support placeholder

        [JsonIgnore]
        [ForeignKey("report_id")]
        public MailReport? Report { get; set; }
    }

    // ─────────────────────────────────────────────
    // 3. Query Sections (Header / Detail / bisa banyak)
    // ─────────────────────────────────────────────
    [Table("RPT_MAIL_SECTION")]
    public class MailReportSection
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int report_id { get; set; }

        [Required, StringLength(50)]
        public required string section_type { get; set; }      // HEADER | DETAIL | SUMMARY

        [Required, StringLength(100)]
        public required string section_label { get; set; }     // Label tampil di email

        public int sort_order { get; set; } = 0;

        // Query SELECT (hanya SELECT, divalidasi)
        [Column(TypeName = "nvarchar(max)")]
        public string? sql_query { get; set; }

        // WHERE clause dinamis — param diambil dari event context
        // e.g. "order_id = {{event.order_id}}"
        [Column(TypeName = "nvarchar(max)")]
        public string? sql_where { get; set; }

        // Tampilan: TABLE | KEY_VALUE | SINGLE_VALUE
        [StringLength(20)]
        public string? display_mode { get; set; } = "TABLE";

        // Kolom yang ingin ditampilkan (comma-separated, kosong = semua)
        [StringLength(500)]
        public string? visible_columns { get; set; }

        // Kolom yang menjadi placeholder di template {{col_name}}
        public byte use_as_placeholder { get; set; } = 0;

        // Grand Total — tampilkan baris total di tfoot untuk kolom numerik
        public byte show_grand_total { get; set; } = 0;

        // Label teks di kolom pertama baris Grand Total (default "Grand Total")
        [StringLength(50)]
        public string? grand_total_label { get; set; } = "Grand Total";

        [JsonIgnore]
        [ForeignKey("report_id")]
        public MailReport? Report { get; set; }
    }

    // ─────────────────────────────────────────────
    // 4. Recipient (link ke VendorTruckEmail per report)
    // ─────────────────────────────────────────────
    [Table("RPT_MAIL_RECIPIENT")]
    public class MailReportRecipient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int report_id { get; set; }

        // Bisa link ke VendorTruckEmail atau manual
        [StringLength(20)]
        public string? recipient_source { get; set; }          // TRUCK_EMAIL | MANUAL

        public int? truck_email_id { get; set; }               // FK ke VendorTruckEmail.ID

        // Manual override
        [StringLength(100)]
        [EmailAddress]
        public string? email_address { get; set; }

        [StringLength(100)]
        public string? email_name { get; set; }

        [Required, StringLength(5)]
        public required string email_type { get; set; }        // TO | CC | BCC

        public byte is_active { get; set; } = 1;

        [JsonIgnore]
        [ForeignKey("report_id")]
        public MailReport? Report { get; set; }

        [JsonIgnore]
        [ForeignKey("truck_email_id")]
        public VendorTruckEmail? TruckEmail { get; set; }
    }

    // ─────────────────────────────────────────────
    // 5. Send Log / History
    // ─────────────────────────────────────────────
    [Table("RPT_MAIL_LOG")]
    public class MailReportLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int report_id { get; set; }

        // Trigger info
        [StringLength(20)]
        public string? trigger_type { get; set; }              // EVENT | SCHEDULED | MANUAL

        //[StringLength(200)]
        //public string? trigger_ref { get; set; }               // e.g. "order_id=123"

        [StringLength(2000)]
        public string? trigger_ref { get; set; }

        // Recipients snapshot
        [Column(TypeName = "nvarchar(max)")]
        public string? recipients_to { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? recipients_cc { get; set; }

        // Content snapshot
        [StringLength(300)]
        public string? subject_sent { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? body_sent { get; set; }                 // full HTML yang dikirim

        // Status: PENDING | SENT | FAILED | RETRYING
        [StringLength(20)]
        public string? status { get; set; } = "PENDING";

        [Column(TypeName = "nvarchar(max)")]
        public string? error_message { get; set; }

        public int retry_count { get; set; } = 0;
        public DateTime? sent_at { get; set; }
        public DateTime created_at { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? triggered_by { get; set; }

        [JsonIgnore]
        [ForeignKey("report_id")]
        public MailReport? Report { get; set; }
    }

    // ─────────────────────────────────────────────
    // ViewModel untuk Form
    // ─────────────────────────────────────────────
    public class MailReportFormVM
    {
        public MailReport Report { get; set; } = new MailReport
        {
            report_code = string.Empty,
            report_name = string.Empty,
            trigger_type = "MANUAL"
        };
        public MailReportTemplate Template { get; set; } = new MailReportTemplate
        {
            subject = string.Empty
        };
        public List<MailReportSection> Sections { get; set; } = new();
        public List<MailReportRecipient> Recipients { get; set; } = new();
    }

    // ─────────────────────────────────────────────
    // ViewModel untuk Preview / Send
    // ─────────────────────────────────────────────
    public class MailSendRequestVM
    {
        public int ReportId { get; set; }
        public Dictionary<string, string> EventParams { get; set; } = new();
        public string? TriggeredBy { get; set; }
        public bool IsPreview { get; set; } = false;
    }

    public class MailPreviewVM
    {
        public string Subject { get; set; } = string.Empty;
        public string BodyHtml { get; set; } = string.Empty;
        public List<string> RecipientsTo { get; set; } = new();
        public List<string> RecipientsCC { get; set; } = new();
        public bool HasPdf { get; set; }
        public bool HasExcel { get; set; }
    }
}