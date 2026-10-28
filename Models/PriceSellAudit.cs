using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models;

[Table("TRC_PRICESELL_AUDIT")]
public class PriceSellAudit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AuditId { get; set; }

    public long ApprovalId { get; set; }

    public long? ApprovalDetailId { get; set; }

    [Required, StringLength(30)]
    public string Action { get; set; } = "";

    [Required, StringLength(50)]
    public string PerformedBy { get; set; } = "";

    public DateTime PerformedAt { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Details { get; set; }
}
