using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("proforma_headers")]
    public class ProformaHeader
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("transaction_no")]
        [MaxLength(30)]
        public string TransactionNo { get; set; } = "";

        [Column("customer_id")]
        [MaxLength(50)]
        public string CustomerId { get; set; } = "";

        [Column("customer_name")]
        [MaxLength(100)]
        public string CustomerName { get; set; } = "";

        [Column("periode_start")]
        public DateTime PeriodeStart { get; set; }

        [Column("periode_end")]
        public DateTime PeriodeEnd { get; set; }

        [Column("approved_by")]
        [MaxLength(100)]
        public string ApprovedBy { get; set; } = "";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<ProformaDetail> Details { get; set; } = new List<ProformaDetail>();
    }

    [Table("proforma_details")]
    public class ProformaDetail
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("header_id")]
        public int HeaderId { get; set; }

        [Column("type")]
        [MaxLength(100)]
        public string Type { get; set; } = "";

        [Column("area")]
        [MaxLength(100)]
        public string Area { get; set; } = "";

        //[Column("grand_total")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        //[Column("approval_value")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ApprovalValue { get; set; }

        [Column("remarks")]
        [MaxLength(500)]
        public string Remarks { get; set; } = "";

        // Navigation
        [ForeignKey("HeaderId")]
        public ProformaHeader? Header { get; set; }
    }
}