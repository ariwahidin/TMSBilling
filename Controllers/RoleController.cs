// Controllers/RoleController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

namespace TMSBilling.Controllers
{
    [SuperAdminOnly]
    public class RoleController : Controller
    {
        private readonly AppDbContext _context;

        public RoleController(AppDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------------
        // INDEX
        // ---------------------------------------------------------------
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _context.Roles
                .Include(r => r.ParentRole)
                .OrderBy(r => r.Id)
                .Select(r => new {
                    r.Id,
                    r.Name,
                    r.Description,
                    ParentRoleName = r.ParentRole != null ? r.ParentRole.Name : "-"
                })
                .ToListAsync();

            return Ok(new { success = true, data = roles });
        }

        // ---------------------------------------------------------------
        // CREATE
        // ---------------------------------------------------------------
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _context.Roles.OrderBy(r => r.Name).ToListAsync();
            return View("Form", new Role());
        }

        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] RoleFormModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { success = false, message = "Name is required." });

            if (await _context.Roles.AnyAsync(r => r.Name == model.Name))
                return BadRequest(new { success = false, message = "Role name already exists." });

            // Validasi: ParentRole tidak boleh membentuk siklus
            // (saat create, tidak ada siklus karena role baru belum punya child)

            var role = new Role
            {
                Name = model.Name,
                Description = model.Description,
                ParentRoleId = model.ParentRoleId,
                CreatedBy = HttpContext.Session.GetString("username"),
                CreatedAt = DateTime.Now,
                UpdatedBy = HttpContext.Session.GetString("username"),
                UpdatedAt = DateTime.Now
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Role created successfully.", data = new { role.Id, role.Name } });
        }

        // ---------------------------------------------------------------
        // EDIT
        // ---------------------------------------------------------------
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();

            // Exclude diri sendiri dan semua descendant dari pilihan parent
            // agar tidak terjadi circular hierarchy
            var descendants = await GetDescendantIdsAsync(id);
            descendants.Add(id);

            ViewBag.Roles = await _context.Roles
                .Where(r => !descendants.Contains(r.Id))
                .OrderBy(r => r.Name)
                .ToListAsync();

            return View("Form", role);
        }

        [HttpPost]
        public async Task<IActionResult> EditAjax([FromBody] RoleFormModel model)
        {
            var role = await _context.Roles.FindAsync(model.Id);
            if (role == null)
                return NotFound(new { success = false, message = "Role not found." });

            if (await _context.Roles.AnyAsync(r => r.Name == model.Name && r.Id != model.Id))
                return BadRequest(new { success = false, message = "Role name already used by another role." });

            // Guard: parent tidak boleh descendant dari diri sendiri
            if (model.ParentRoleId.HasValue)
            {
                var descendants = await GetDescendantIdsAsync(model.Id);
                if (descendants.Contains(model.ParentRoleId.Value))
                    return BadRequest(new { success = false, message = "Circular hierarchy detected." });
            }

            role.Name = model.Name;
            role.Description = model.Description;
            role.ParentRoleId = model.ParentRoleId;
            role.UpdatedBy = HttpContext.Session.GetString("username");
            role.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Role updated successfully." });
        }

        // ---------------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> DeleteAjax([FromBody] DeleteModel model)
        {
            var role = await _context.Roles.FindAsync(model.Id);
            if (role == null)
                return NotFound(new { success = false, message = "Role not found." });

            // Cek apakah role masih dipakai
            var inUse = await _context.UserRoles.AnyAsync(ur => ur.RoleId == model.Id);
            if (inUse)
                return BadRequest(new { success = false, message = "Cannot delete: role is still assigned to users." });

            var hasChildren = await _context.Roles.AnyAsync(r => r.ParentRoleId == model.Id);
            if (hasChildren)
                return BadRequest(new { success = false, message = "Cannot delete: role has child roles." });

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Role deleted successfully." });
        }

        // ---------------------------------------------------------------
        // PERMISSION MATRIX
        // ---------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Permissions(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();

            var allPermissions = await _context.Permissions.OrderBy(p => p.Id).ToListAsync();
            var assignedIds = await _context.RolePermissions
                .Where(rp => rp.RoleId == id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            ViewBag.Role = role;
            ViewBag.AllPermissions = allPermissions;
            ViewBag.AssignedIds = assignedIds;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SavePermissions([FromBody] SavePermissionsModel model)
        {
            var role = await _context.Roles.FindAsync(model.RoleId);
            if (role == null)
                return NotFound(new { success = false, message = "Role not found." });

            // Hapus semua permission lama role ini, replace dengan yang baru
            var existing = _context.RolePermissions.Where(rp => rp.RoleId == model.RoleId);
            _context.RolePermissions.RemoveRange(existing);

            if (model.PermissionIds?.Count > 0)
            {
                var newPermissions = model.PermissionIds.Select((permId, index) => new RolePermission
                {
                    RoleId = model.RoleId,
                    PermissionId = permId
                });
                await _context.RolePermissions.AddRangeAsync(newPermissions);
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Permissions saved successfully." });
        }

        // ---------------------------------------------------------------
        // HELPER
        // ---------------------------------------------------------------
        private async Task<HashSet<int>> GetDescendantIdsAsync(int roleId)
        {
            var allRoles = await _context.Roles.ToListAsync();
            var result = new HashSet<int>();
            CollectDescendants(roleId, allRoles, result);
            return result;
        }

        private void CollectDescendants(int roleId, List<Role> allRoles, HashSet<int> result)
        {
            var children = allRoles.Where(r => r.ParentRoleId == roleId);
            foreach (var child in children)
            {
                if (result.Add(child.Id))
                    CollectDescendants(child.Id, allRoles, result);
            }
        }

        // ---------------------------------------------------------------
        // DTO
        // ---------------------------------------------------------------
        public class RoleFormModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string? Description { get; set; }
            public int? ParentRoleId { get; set; }
        }

        public class DeleteModel
        {
            public int Id { get; set; }
        }

        public class SavePermissionsModel
        {
            public int RoleId { get; set; }
            public List<int>? PermissionIds { get; set; }
        }
    }
}