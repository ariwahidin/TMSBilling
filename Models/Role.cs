// Models/Role.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(255)]
        public string? Description { get; set; }

        // Self-reference untuk hierarki
        public int? ParentRoleId { get; set; }

        [ForeignKey("ParentRoleId")]
        public Role? ParentRole { get; set; }

        // Navigation
        public ICollection<Role> ChildRoles { get; set; } = new List<Role>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}