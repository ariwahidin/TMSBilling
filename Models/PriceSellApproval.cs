using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models;

[Table("TRC_PRICESELL_APPROVAL")]
public class PriceSellApproval
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long ApprovalId { get; set; }

    [Required, StringLength(30)]
    public string RequestNo { get; set; } = "";

    [Required, StringLength(20)]
    public string RequestType { get; set; } = ""; // CREATE, UPDATE, DEACTIVATE, IMPORT

    [Required, StringLength(20)]
    public string Status { get; set; } = "PENDING";

    [Required, StringLength(50)]
    public string Requester { get; set; } = "";

    public DateTime RequestedAt { get; set; }

    [StringLength(50)]
    public string? Approver { get; set; }

    public DateTime? DecidedAt { get; set; }

    [StringLength(1000)]
    public string? DecisionNote { get; set; }

    [StringLength(260)]
    public string? SourceFileName { get; set; }

    public int RowCount { get; set; }

    public DateTime? AppliedAt { get; set; }


    public ICollection<PriceSellApprovalDetail> Details { get; set; } = new List<PriceSellApprovalDetail>();
}
