using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("EmailSettings")]
    public class EmailSettings
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string SmtpHost { get; set; } = string.Empty;

        [Required]
        public int SmtpPort { get; set; } = 587;

        [Required]
        [StringLength(100)]
        public string FromEmail { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DisplayName { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty; // akan di-encrypt

        public bool UseSSL { get; set; } = true;

        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
