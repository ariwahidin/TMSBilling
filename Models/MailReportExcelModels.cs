using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TMSBilling.Models
{
    // ─────────────────────────────────────────────
    // 1. Excel Layout — 1 per report
    // ─────────────────────────────────────────────
    [Table("RPT_EXCEL_LAYOUT")]
    public class MailReportExcelLayout
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int report_id { get; set; }

        public byte use_custom_layout { get; set; } = 0;

        [StringLength(300)]
        public string? report_title { get; set; }

        [StringLength(10)]
        public string? title_bg_color { get; set; } = "FFFFFF";

        [StringLength(10)]
        public string? title_font_color { get; set; } = "000000";

        public int title_font_size { get; set; } = 14;

        [JsonIgnore]
        [ForeignKey("report_id")]
        public MailReport? Report { get; set; }

        public List<MailReportExcelSheet> Sheets { get; set; } = new();
    }

    // ─────────────────────────────────────────────
    // 2. Excel Sheet — bisa banyak per layout
    // ─────────────────────────────────────────────
    [Table("RPT_EXCEL_SHEET")]
    public class MailReportExcelSheet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int layout_id { get; set; }

        [Required, StringLength(100)]
        public required string sheet_name { get; set; }

        public int sort_order { get; set; } = 0;

        [JsonIgnore]
        [ForeignKey("layout_id")]
        public MailReportExcelLayout? Layout { get; set; }

        public List<MailReportExcelSection> Sections { get; set; } = new();
    }

    // ─────────────────────────────────────────────
    // 3. Excel Section — bisa banyak per sheet
    // ─────────────────────────────────────────────
    [Table("RPT_EXCEL_SECTION")]
    public class MailReportExcelSection
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int sheet_id { get; set; }

        [Required, StringLength(100)]
        public required string section_label { get; set; }

        public int sort_order { get; set; } = 0;

        [Column(TypeName = "nvarchar(max)")]
        public string? sql_query { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? sql_where { get; set; }

        [StringLength(1000)]
        public string? visible_columns { get; set; }

        // "vertical" | "side_by_side"
        [StringLength(20)]
        public string? layout { get; set; } = "vertical";

        // Section dengan group_id sama + layout side_by_side = berdampingan
        public int group_id { get; set; } = 0;

        // Jumlah kolom kosong antar section side_by_side
        public int side_gap { get; set; } = 1;

        // Jumlah baris kosong antar blok/group
        public int bottom_gap { get; set; } = 2;

        [StringLength(300)]
        public string? section_title { get; set; }

        [StringLength(10)]
        public string? title_bg_color { get; set; } = "FFD700";

        [StringLength(10)]
        public string? title_font_color { get; set; } = "000000";

        [StringLength(10)]
        public string? header_bg_color { get; set; } = "FFD700";

        [StringLength(10)]
        public string? header_font_color { get; set; } = "000000";

        public byte show_grand_total { get; set; } = 0;

        [StringLength(50)]
        public string? grand_total_label { get; set; } = "TOTAL";

        [StringLength(10)]
        public string? total_bg_color { get; set; } = "FFD700";

        // "TABLE" | "KEY_VALUE"
        [StringLength(20)]
        public string? display_mode { get; set; } = "TABLE";

        [JsonIgnore]
        [ForeignKey("sheet_id")]
        public MailReportExcelSheet? Sheet { get; set; }
    }

    // ─────────────────────────────────────────────
    // ViewModels
    // ─────────────────────────────────────────────
    public class MailReportExcelLayoutVM
    {
        public MailReportExcelLayout Layout { get; set; } = new MailReportExcelLayout();
        public List<MailReportExcelSheetVM> Sheets { get; set; } = new();
    }

    public class MailReportExcelSheetVM
    {
        public MailReportExcelSheet Sheet { get; set; } = new MailReportExcelSheet { sheet_name = "Sheet1" };
        public List<MailReportExcelSection> Sections { get; set; } = new();
    }

    public class SaveExcelLayoutRequest
    {
        public int ReportId { get; set; }
        public MailReportExcelLayoutVM Layout { get; set; } = new();
    }
}