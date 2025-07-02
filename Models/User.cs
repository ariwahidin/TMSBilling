using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TMSBilling.Models
{

    public class User
    {

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Username wajib diisi.")]
        [StringLength(100, ErrorMessage = "Nama tidak boleh lebih dari 100 karakter.")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Password wajib diisi.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter.")]
        public string Password { get; set; } = "";

        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }

        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
