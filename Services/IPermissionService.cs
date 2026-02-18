// Services/IPermissionService.cs
namespace TMSBilling.Services
{
    public interface IPermissionService
    {
        /// <summary>
        /// Resolve semua effective permission user berdasarkan roles + hierarki,
        /// lalu simpan ke Session.
        /// </summary>
        Task ResolveAndStorePermissionsAsync(int userId);

        /// <summary>
        /// Cek apakah user yang sedang login punya permission tertentu.
        /// SuperAdmin selalu return true.
        /// </summary>
        bool HasPermission(string permissionCode);

        /// <summary>
        /// Cek apakah user yang sedang login adalah SuperAdmin.
        /// </summary>
        bool IsSuperAdmin();

        /// <summary>
        /// Ambil semua menu yang boleh dilihat user, sudah terstruktur parent-child.
        /// </summary>
        Task<List<MenuNode>> GetAccessibleMenusAsync();
    }

    // DTO untuk menu terstruktur
    public class MenuNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Url { get; set; }
        public string? Icon { get; set; }
        public int OrderIndex { get; set; }
        public List<MenuNode> Children { get; set; } = new();
    }
}