using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("IntegrationRecipients")]
    public class IntegrationRecipient
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Integration))]
        public int IntegrationId { get; set; }
        public Integration? Integration { get; set; }

        [Required, MaxLength(500)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Name { get; set; }

        /// <summary>to | cc | bcc</summary>
        [MaxLength(10)]
        public string RecipientType { get; set; } = "to";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("IntegrationHistories")]
    public class IntegrationHistory
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Integration))]
        public int IntegrationId { get; set; }
        public Integration? Integration { get; set; }

        [Required, MaxLength(50)]
        public string EventKey { get; set; } = string.Empty;

        /// <summary>success | failed</summary>
        [Required, MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        /// <summary>realtime | scheduled</summary>
        [MaxLength(20)]
        public string? TriggerType { get; set; }

        /// <summary>JSON snapshot of event data that triggered this run</summary>
        public string? EventDataJson { get; set; }

        /// <summary>Human-readable result message</summary>
        public string? Message { get; set; }

        public string? ErrorDetail { get; set; }

        /// <summary>Number of rows sent/processed</summary>
        public int? RowCount { get; set; }

        public long? DurationMs { get; set; }

        public bool EmailSent { get; set; } = false;
        public string? EmailError { get; set; }

        public DateTime ExecutedAt { get; set; } = DateTime.Now;
    }
}
