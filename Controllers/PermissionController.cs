// Controllers/PermissionController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Models;
using TMSBilling.Filters;

namespace TMSBilling.Controllers
{
    [SuperAdminOnly]
    public class PermissionController : Controller
    {
        private readonly AppDbContext _context;

        public PermissionController(AppDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------------
        // INDEX
        // ---------------------------------------------------------------
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetPermissions()
        {
            var permissions = await _context.Permissions
                .OrderBy(p => p.Id)
                .Select(p => new {
                    p.Id,
                    p.Code,
                    p.Name,
                    p.Description,
                    UsedByRoles = p.RolePermissions.Count,
                    UsedByMenus = p.Menus.Count
                })
                .ToListAsync();

            return Ok(new { success = true, data = permissions });
        }

        // ---------------------------------------------------------------
        // CREATE
        // ---------------------------------------------------------------
        public IActionResult Create() => View("Form", new Permission());

        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] PermissionFormModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { success = false, message = "Code and Name are required." });

            // Code harus unik
            if (await _context.Permissions.AnyAsync(p => p.Code == model.Code))
                return BadRequest(new { success = false, message = "Permission code already exists." });

            // Code hanya boleh huruf kecil, angka, dan underscore
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.Code, @"^[a-z0-9_]+$"))
                return BadRequest(new { success = false, message = "Code only allows lowercase letters, numbers, and underscores." });

            var permission = new Permission
            {
                Code = model.Code.Trim(),
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim()
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Permission created successfully." });
        }

        // ---------------------------------------------------------------
        // EDIT
        // ---------------------------------------------------------------
        public async Task<IActionResult> Edit(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null) return NotFound();
            return View("Form", permission);
        }

        [HttpPost]
        public async Task<IActionResult> EditAjax([FromBody] PermissionFormModel model)
        {
            var permission = await _context.Permissions.FindAsync(model.Id);
            if (permission == null)
                return NotFound(new { success = false, message = "Permission not found." });

            // Cek duplikat code di permission lain
            if (await _context.Permissions.AnyAsync(p => p.Code == model.Code && p.Id != model.Id))
                return BadRequest(new { success = false, message = "Permission code already used by another permission." });

            if (!System.Text.RegularExpressions.Regex.IsMatch(model.Code, @"^[a-z0-9_]+$"))
                return BadRequest(new { success = false, message = "Code only allows lowercase letters, numbers, and underscores." });

            // Jika code berubah, update juga semua menu yang pakai code lama
            var oldCode = permission.Code;
            var codeChanged = oldCode != model.Code.Trim();

            permission.Code = model.Code.Trim();
            permission.Name = model.Name.Trim();
            permission.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

            if (codeChanged)
            {
                // Update PermissionCode di tabel Menus yang referensi code lama
                var affectedMenus = await _context.Menus
                    .Where(m => m.PermissionCode == oldCode)
                    .ToListAsync();

                foreach (var menu in affectedMenus)
                    menu.PermissionCode = model.Code.Trim();
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Permission updated successfully." });
        }

        // ---------------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> DeleteAjax([FromBody] DeleteModel model)
        {
            var permission = await _context.Permissions.FindAsync(model.Id);
            if (permission == null)
                return NotFound(new { success = false, message = "Permission not found." });

            // Cek apakah masih dipakai role
            var usedByRole = await _context.RolePermissions.AnyAsync(rp => rp.PermissionId == model.Id);
            if (usedByRole)
                return BadRequest(new { success = false, message = "Cannot delete: permission is still assigned to one or more roles." });

            // Cek apakah masih dipakai menu
            var usedByMenu = await _context.Menus.AnyAsync(m => m.PermissionCode == permission.Code);
            if (usedByMenu)
                return BadRequest(new { success = false, message = "Cannot delete: permission is still used by one or more menus." });

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Permission deleted successfully." });
        }

        // ---------------------------------------------------------------
        // DTO
        // ---------------------------------------------------------------
        public class PermissionFormModel
        {
            public int Id { get; set; }
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string? Description { get; set; }
        }

        public class DeleteModel
        {
            public int Id { get; set; }
        }
    }
}