using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models;

[Table("TRC_PRICESELL_APPROVAL_DETAIL")]
public class PriceSellApprovalDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long ApprovalDetailId { get; set; }

    public long ApprovalId { get; set; }

    [ForeignKey(nameof(ApprovalId))]
    public PriceSellApproval? Approval { get; set; }

    [Required, StringLength(20)]
    public string OperationType { get; set; } = ""; // CREATE, UPDATE, DEACTIVATE

    public int? PriceSellId { get; set; }

    // Business identity key used to prevent duplicate pending requests.
    [Required, StringLength(500)]
    public string ConflictKey { get; set; } = "";

    [Column(TypeName = "nvarchar(max)")]
    public string? OldValuesJson { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string NewValuesJson { get; set; } = "";

    // Set only while this detail belongs to a pending request.
    // A filtered unique index on this column prevents concurrent pending requests
    // for the same tariff identity. Null for terminal requests.
    [StringLength(500)]
    public string? PendingConflictKey { get; set; }
}
