using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models;

[Table("TRC_PRICEBUY_APPROVAL_H")]
public class PriceBuyApproval
{
    [Key] public long ApprovalId { get; set; }
    [Required, StringLength(40)] public string RequestNo { get; set; } = "";
    [Required, StringLength(20)] public string RequestType { get; set; } = "";
    [Required, StringLength(20)] public string Status { get; set; } = "PENDING";
    [Required, StringLength(50)] public string Requester { get; set; } = "";
    public DateTime RequestedAt { get; set; }
    [StringLength(50)] public string? Approver { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime? AppliedAt { get; set; }
    [StringLength(1000)] public string? DecisionNote { get; set; }
    [StringLength(260)] public string? SourceFileName { get; set; }
    public int RowCount { get; set; }
    public ICollection<PriceBuyApprovalDetail> Details { get; set; } = new List<PriceBuyApprovalDetail>();
}

[Table("TRC_PRICEBUY_APPROVAL_D")]
public class PriceBuyApprovalDetail
{
    [Key] public long DetailId { get; set; }
    public long ApprovalId { get; set; }
    [ForeignKey(nameof(ApprovalId))] public PriceBuyApproval? Approval { get; set; }
    [Required, StringLength(20)] public string OperationType { get; set; } = "";
    public int? PriceBuyId { get; set; }
    [Required, StringLength(64)] public string ConflictKey { get; set; } = "";
    [StringLength(64)] public string? PendingConflictKey { get; set; }
    public string? OldValuesJson { get; set; }
    [Required] public string NewValuesJson { get; set; } = "";
}

[Table("TRC_PRICEBUY_APPROVAL_AUDIT")]
public class PriceBuyAudit
{
    [Key] public long AuditId { get; set; }
    public long ApprovalId { get; set; }
    [Required, StringLength(40)] public string Action { get; set; } = "";
    [Required, StringLength(50)] public string PerformedBy { get; set; } = "";
    public DateTime PerformedAt { get; set; }
    [StringLength(2000)] public string? Details { get; set; }
}

public sealed class PriceBuyApprovalSubmitDto
{
    public string RequestType { get; set; } = "";
    public string? SourceFileName { get; set; }
    public List<PriceBuyApprovalItemDto> Items { get; set; } = new();
}

public sealed class PriceBuyApprovalItemDto
{
    public string OperationType { get; set; } = "";
    public int? PriceBuyId { get; set; }
    public string NewValuesJson { get; set; } = "";
}
