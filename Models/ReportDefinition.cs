using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TMSBilling.Models
{
    // ─────────────────────────────────────────────
    // 1. Report Definition
    //    Master definisi report yang bisa di-generate user
    // ─────────────────────────────────────────────
    [Table("RPT_DEFINITION")]
    public class ReportDefinition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required, StringLength(50)]
        public required string report_code { get; set; }       // e.g. "DAILY_DELIVERY"

        [Required, StringLength(200)]
        public required string report_name { get; set; }       // e.g. "Daily Delivery Report"

        [StringLength(500)]
        public string? report_desc { get; set; }

        // Kategori untuk grouping di menu (e.g. "Delivery", "POD", "Finance")
        [StringLength(100)]
        public string? category { get; set; }

        // Icon Bootstrap Icons e.g. "bi-truck", "bi-file-earmark-excel"
        [StringLength(50)]
        public string? icon { get; set; } = "bi-file-earmark-bar-graph";

        // Output format yang diizinkan: "excel,csv,pdf" (comma-separated)
        [StringLength(50)]
        public string? allowed_outputs { get; set; } = "excel,csv";

        // Default output format
        [StringLength(10)]
        public string? default_output { get; set; } = "excel";

        // Filename template, support {{placeholder}}
        // e.g. "Daily_Delivery_{{date_from}}_{{date_to}}"
        [StringLength(200)]
        public string? filename_template { get; set; }

        // Excel layout ID — link ke RPT_EXCEL_LAYOUT
        // NULL = pakai flat single-sheet export
        public int? excel_layout_id { get; set; }

        public byte is_active { get; set; } = 1;

        [StringLength(50)]
        public string? entry_user { get; set; }
        public DateTime? entry_date { get; set; }
        [StringLength(50)]
        public string? update_user { get; set; }
        public DateTime? update_date { get; set; }

        // Navigation
        [JsonIgnore]
        public List<ReportParam> Params { get; set; } = new();

        [JsonIgnore]
        public List<ReportPermission> Permissions { get; set; } = new();
    }

    // ─────────────────────────────────────────────
    // 2. Report Parameter
    //    Definisi parameter yang muncul di form user
    // ─────────────────────────────────────────────
    [Table("RPT_PARAM")]
    public class ReportParam
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int report_id { get; set; }

        // Key untuk placeholder di SQL e.g. "date_from" → {{date_from}}
        [Required, StringLength(50)]
        public required string param_key { get; set; }

        // Label yang tampil di form
        [Required, StringLength(100)]
        public required string param_label { get; set; }

        // Type: date | text | number | select | multiselect | hidden | daterange
        [Required, StringLength(20)]
        public required string param_type { get; set; }

        // Untuk type=select/multiselect:
        // Bisa hardcode "ACTIVE,INACTIVE" atau query "SELECT id, name FROM tbl"
        [Column(TypeName = "nvarchar(max)")]
        public string? param_options { get; set; }

        // Source options: "static" | "query"
        [StringLength(10)]
        public string? options_source { get; set; } = "static";

        // Value kolom untuk option value (kalau query)
        [StringLength(50)]
        public string? options_value_col { get; set; }

        // Text kolom untuk option display (kalau query)
        [StringLength(50)]
        public string? options_text_col { get; set; }

        // Default value, support {{today}}, {{month_start}}, {{month_end}}
        [StringLength(200)]
        public string? default_value { get; set; }

        // Placeholder text di input
        [StringLength(100)]
        public string? placeholder { get; set; }

        public byte is_required { get; set; } = 0;

        // Hidden = tidak tampil di form, pakai default_value langsung
        public byte is_hidden { get; set; } = 0;

        public int sort_order { get; set; } = 0;

        // Lebar kolom di form: 1-12 (Bootstrap grid)
        public int col_width { get; set; } = 3;

        [JsonIgnore]
        [ForeignKey("report_id")]
        public ReportDefinition? Report { get; set; }
    }

    // ─────────────────────────────────────────────
    // 3. Report Permission
    //    Role mana yang boleh akses report ini
    // ─────────────────────────────────────────────
    [Table("RPT_PERMISSION")]
    public class ReportPermission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int report_id { get; set; }

        // Role name atau "ALL" untuk semua role
        [Required, StringLength(50)]
        public required string role_name { get; set; }

        [JsonIgnore]
        [ForeignKey("report_id")]
        public ReportDefinition? Report { get; set; }
    }

    // ─────────────────────────────────────────────
    // 4. Report Run Log
    //    Audit trail setiap kali user generate report
    // ─────────────────────────────────────────────
    [Table("RPT_RUN_LOG")]
    public class ReportRunLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int report_id { get; set; }

        [StringLength(50)]
        public string? run_by { get; set; }

        // Parameter yang digunakan (JSON)
        [Column(TypeName = "nvarchar(max)")]
        public string? params_json { get; set; }

        // Output format: excel | csv | pdf
        [StringLength(10)]
        public string? output_format { get; set; }

        // Status: SUCCESS | FAILED
        [StringLength(20)]
        public string? status { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? error_message { get; set; }

        public int? row_count { get; set; }
        public int? duration_ms { get; set; }

        public DateTime run_at { get; set; } = DateTime.Now;

        [JsonIgnore]
        [ForeignKey("report_id")]
        public ReportDefinition? Report { get; set; }
    }

    // ─────────────────────────────────────────────
    // ViewModels
    // ─────────────────────────────────────────────
    public class ReportBuilderFormVM
    {
        public ReportDefinition Report { get; set; } = new ReportDefinition
        {
            report_code = "",
            report_name = "",
            allowed_outputs = "excel,csv",
            default_output = "excel"
        };
        public List<ReportParam> Params { get; set; } = new();
        public List<ReportPermission> Permissions { get; set; } = new();
        public MailReportExcelLayoutVM? ExcelLayout { get; set; }
    }

    public class ReportGenerateRequest
    {
        public int ReportId { get; set; }
        public Dictionary<string, string> Params { get; set; } = new();
        public string OutputFormat { get; set; } = "excel"; // excel | csv | pdf
    }

    public class ReportParamOptionItem
    {
        public string Value { get; set; } = "";
        public string Text { get; set; } = "";
    }
}