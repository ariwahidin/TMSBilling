// Models/Permission.cs
using System.ComponentModel.DataAnnotations;

namespace TMSBilling.Models
{
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Code { get; set; } = ""; // e.g. "user", "invoice"

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = ""; // e.g. "User Management"

        [StringLength(255)]
        public string? Description { get; set; }

        // Navigation
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<Menu> Menus { get; set; } = new List<Menu>();
    }
}