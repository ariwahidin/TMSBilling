using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    /// <summary>
    /// Konfigurasi attachment email per integrasi.
    /// Multiple attachment bisa digabung jadi 1 file Excel multi-sheet
    /// dengan menggunakan GroupFileKey yang sama.
    /// </summary>
    [Table("IntegrationAttachments")]
    public class IntegrationAttachment
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Integration))]
        public int IntegrationId { get; set; }
        public Integration? Integration { get; set; }

        // ── Type ─────────────────────────────────────────────────────────────
        /// <summary>query | file_path</summary>
        [Required, MaxLength(20)]
        public string AttachmentType { get; set; } = "query";

        // ── Grouping & File Info ──────────────────────────────────────────────
        /// <summary>
        /// Attachment dengan GroupFileKey yang sama akan digabung menjadi
        /// 1 file Excel multi-sheet. Beda GroupFileKey = file terpisah.
        /// Contoh: "laporan_harian", "spk"
        /// </summary>
        [Required, MaxLength(100)]
        public string GroupFileKey { get; set; } = string.Empty;

        /// <summary>
        /// Nama file output. Hanya dipakai dari attachment pertama (SheetOrder=1)
        /// dalam setiap group. Support placeholder: {{date}}, {{order_no}}, dll.
        /// Contoh: "Laporan_Harian_{{date}}.xlsx"
        /// </summary>
        [MaxLength(500)]
        public string? GroupFileName { get; set; }

        /// <summary>csv | xlsx | json | txt — dipakai dari attachment pertama dalam group</summary>
        [MaxLength(20)]
        public string FileFormat { get; set; } = "xlsx";

        // ── Sheet Config (untuk query type, terutama xlsx multi-sheet) ─────────
        /// <summary>Nama tab sheet di Excel. Contoh: "Daftar Order"</summary>
        [MaxLength(100)]
        public string? SheetName { get; set; }

        /// <summary>Urutan sheet dalam file (1-based). Lebih kecil = lebih kiri.</summary>
        public int SheetOrder { get; set; } = 1;

        // ── Query (AttachmentType = "query") ──────────────────────────────────
        /// <summary>
        /// SQL SELECT query. Support CTE, JOIN, UNION, subquery, ORDER BY.
        /// Placeholder {{field}} dari eventData bisa dipakai sebagai parameter.
        /// </summary>
        public string? Query { get; set; }

        // ── File Path (AttachmentType = "file_path") ──────────────────────────
        /// <summary>
        /// Path file di server. Support placeholder: {{date}}, {{order_no}}, dll.
        /// Contoh: "C:\Reports\Template_{{date}}.pdf"
        /// </summary>
        [MaxLength(1000)]
        public string? FilePath { get; set; }

        // ── Display ───────────────────────────────────────────────────────────
        /// <summary>Nama deskriptif yang tampil di UI. Contoh: "Daftar Order Hari Ini"</summary>
        [MaxLength(200)]
        public string? DisplayName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
