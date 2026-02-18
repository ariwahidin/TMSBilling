// Models/Menu.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    public class Menu
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(255)]
        public string? Url { get; set; } // null = menu group

        [StringLength(100)]
        public string? Icon { get; set; }

        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public Menu? ParentMenu { get; set; }

        // FK ke Permission.Code (bukan Id, agar lebih readable di razor)
        [StringLength(100)]
        public string? PermissionCode { get; set; }

        [ForeignKey("PermissionCode")]
        public Permission? Permission { get; set; }

        public int OrderIndex { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Menu> Children { get; set; } = new List<Menu>();
    }
}