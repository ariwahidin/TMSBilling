using System.ComponentModel.DataAnnotations;

namespace TMSBilling.Models.ViewModels
{
    public class EmailSettingsViewModel
    {
        [Required(ErrorMessage = "SMTP Host wajib diisi.")]
        [StringLength(100)]
        [Display(Name = "SMTP Host")]
        public string SmtpHost { get; set; } = string.Empty;

        [Required(ErrorMessage = "SMTP Port wajib diisi.")]
        [Display(Name = "SMTP Port")]
        public int SmtpPort { get; set; } = 587;

        [Required(ErrorMessage = "From Email wajib diisi.")]
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        [StringLength(100)]
        [Display(Name = "From Email")]
        public string FromEmail { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Display Name")]
        public string? DisplayName { get; set; }

        [Required(ErrorMessage = "Username wajib diisi.")]
        [StringLength(100)]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        // Tidak Required supaya saat edit, password lama tetap tersimpan jika dikosongkan
        [StringLength(100)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Display(Name = "Gunakan SSL")]
        public bool UseSSL { get; set; } = true;

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }
}