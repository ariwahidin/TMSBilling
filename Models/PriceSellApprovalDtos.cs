using System.ComponentModel.DataAnnotations;

namespace TMSBilling.Models;

public sealed class PriceSellApprovalSubmitDto
{
    [Required]
    public string RequestType { get; set; } = ""; // CREATE, UPDATE, DEACTIVATE, IMPORT

    public string? SourceFileName { get; set; }

    [Required, MinLength(1)]
    public List<PriceSellApprovalItemDto> Items { get; set; } = new();
}

public sealed class PriceSellApprovalItemDto
{
    public string OperationType { get; set; } = ""; // CREATE, UPDATE, DEACTIVATE

    public int? PriceSellId { get; set; }

    // JSON representing PriceSell fields. Server deserializes and validates it.
    public string NewValuesJson { get; set; } = "";
}

public sealed class PriceSellDecisionDto
{
    [StringLength(1000)]
    public string? Note { get; set; }
}
