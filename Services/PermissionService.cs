// Services/PermissionService.cs
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Models;

namespace TMSBilling.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Key yang dipakai di Session
        private const string SESSION_PERMISSIONS = "permissions";
        private const string SESSION_IS_SUPERADMIN = "is_superadmin";

        public PermissionService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // ---------------------------------------------------------------
        // RESOLVE & STORE — dipanggil sekali saat login
        // ---------------------------------------------------------------
        public async Task ResolveAndStorePermissionsAsync(int userId)
        {
            var session = _httpContextAccessor.HttpContext!.Session;

            // 1. Ambil semua role yang dimiliki user
            var roleIds = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            // 2. Cek apakah salah satu role adalah SuperAdmin
            var roleNames = await _context.Roles
                .Where(r => roleIds.Contains(r.Id))
                .Select(r => r.Name)
                .ToListAsync();

            if (roleNames.Contains("SuperAdmin"))
            {
                session.SetString(SESSION_IS_SUPERADMIN, "true");
                session.SetString(SESSION_PERMISSIONS, "*");
                return;
            }

            session.SetString(SESSION_IS_SUPERADMIN, "false");

            // 3. Load semua role dari DB sekaligus (untuk resolve hierarki in-memory)
            var allRoles = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .ToListAsync();

            // 4. Resolve effective permissions secara rekursif untuk setiap role
            var effectivePermissions = new HashSet<string>();

            foreach (var roleId in roleIds)
            {
                var perms = ResolvePermissionsRecursive(roleId, allRoles, new HashSet<int>());
                foreach (var p in perms)
                    effectivePermissions.Add(p);
            }

            // 5. Simpan ke Session sebagai comma-separated string
            session.SetString(SESSION_PERMISSIONS, string.Join(",", effectivePermissions));
        }

        // ---------------------------------------------------------------
        // RECURSIVE RESOLVER — berjalan in-memory, tidak query DB lagi
        // ---------------------------------------------------------------
        private HashSet<string> ResolvePermissionsRecursive(
            int roleId,
            List<Role> allRoles,
            HashSet<int> visited) // guard infinite loop jika data melingkar
        {
            if (visited.Contains(roleId))
                return new HashSet<string>();

            visited.Add(roleId);

            var role = allRoles.FirstOrDefault(r => r.Id == roleId);
            if (role == null)
                return new HashSet<string>();

            // Permission langsung di role ini
            var permissions = role.RolePermissions
                .Select(rp => rp.Permission.Code)
                .ToHashSet();

            // Inherit dari parent role
            if (role.ParentRoleId.HasValue)
            {
                var parentPerms = ResolvePermissionsRecursive(
                    role.ParentRoleId.Value, allRoles, visited);

                foreach (var p in parentPerms)
                    permissions.Add(p);
            }

            return permissions;
        }

        // ---------------------------------------------------------------
        // HAS PERMISSION — dipakai di Controller & View
        // ---------------------------------------------------------------
        public bool HasPermission(string permissionCode)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return false;

            // SuperAdmin bypass semua
            if (IsSuperAdmin()) return true;

            var stored = session.GetString(SESSION_PERMISSIONS) ?? "";
            return stored.Split(',').Contains(permissionCode);
        }

        // ---------------------------------------------------------------
        // IS SUPERADMIN
        // ---------------------------------------------------------------
        public bool IsSuperAdmin()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            return session?.GetString(SESSION_IS_SUPERADMIN) == "true";
        }

        // ---------------------------------------------------------------
        // GET ACCESSIBLE MENUS — untuk render navigasi di layout
        // ---------------------------------------------------------------
        public async Task<List<MenuNode>> GetAccessibleMenusAsync()
        {
            // Ambil semua menu aktif dari DB sekaligus
            var allMenus = await _context.Menus
                .Where(m => m.IsActive)
                .OrderBy(m => m.OrderIndex)
                .ToListAsync();

            // Filter berdasarkan permission, lalu susun parent-child
            return BuildMenuTree(allMenus, parentId: null);
        }

        private List<MenuNode> BuildMenuTree(List<Menu> allMenus, int? parentId)
        {
            var nodes = new List<MenuNode>();

            var children = allMenus
                .Where(m => m.ParentId == parentId)
                .OrderBy(m => m.OrderIndex);

            foreach (var menu in children)
            {
                // Cek permission menu ini
                if (!string.IsNullOrEmpty(menu.PermissionCode) && !HasPermission(menu.PermissionCode))
                    continue;

                var node = new MenuNode
                {
                    Id = menu.Id,
                    Name = menu.Name,
                    Url = menu.Url,
                    Icon = menu.Icon,
                    OrderIndex = menu.OrderIndex,
                    Children = BuildMenuTree(allMenus, menu.Id)
                };

                // Menu group (tanpa URL): tampilkan hanya jika ada child yang accessible
                if (menu.Url == null && node.Children.Count == 0)
                    continue;

                nodes.Add(node);
            }

            return nodes;
        }
    }
}