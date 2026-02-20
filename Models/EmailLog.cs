using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("EmailLogs")]
    public class EmailLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(30)]
        public string? JobId { get; set; }          // relasi ke JobHeader.jobid

        [Required]
        [StringLength(1000)]
        public string ToEmail { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? CcEmail { get; set; }

        [Required]
        [StringLength(100)]
        public string FromEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string TriggerType { get; set; } = string.Empty;   // 'manual' / 'auto'

        [StringLength(30)]
        public string? TriggerStatus { get; set; }                // 'SCHEDULED' / 'DELIVERED'

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsSuccess { get; set; } = false;

        public string? ErrorMessage { get; set; }

        public int? SentByUserId { get; set; }                    // NULL jika auto

        // Navigation property
        [ForeignKey("SentByUserId")]
        public User? SentByUser { get; set; }
    }
}
