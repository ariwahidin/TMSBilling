// Controllers/MenuController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Models;
using TMSBilling.Filters;

namespace TMSBilling.Controllers
{
    [SuperAdminOnly]
    public class MenuController : Controller
    {
        private readonly AppDbContext _context;

        public MenuController(AppDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------------
        // INDEX
        // ---------------------------------------------------------------
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetMenus()
        {
            var menus = await _context.Menus
                .Include(m => m.ParentMenu)
                .OrderBy(m => m.ParentId)
                .ThenBy(m => m.OrderIndex)
                .Select(m => new {
                    m.Id,
                    m.Name,
                    m.Url,
                    m.Icon,
                    m.PermissionCode,
                    m.OrderIndex,
                    m.IsActive,
                    ParentName = m.ParentMenu != null ? m.ParentMenu.Name : "-"
                })
                .ToListAsync();

            return Ok(new { success = true, data = menus });
        }

        // ---------------------------------------------------------------
        // CREATE
        // ---------------------------------------------------------------
        public async Task<IActionResult> Create()
        {
            await PopulateViewBag();
            return View("Form", new Menu());
        }

        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] MenuFormModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { success = false, message = "Name is required." });

            var menu = new Menu
            {
                Name = model.Name,
                Url = string.IsNullOrWhiteSpace(model.Url) ? null : model.Url,
                Icon = model.Icon,
                ParentId = model.ParentId,
                PermissionCode = string.IsNullOrWhiteSpace(model.PermissionCode) ? null : model.PermissionCode,
                OrderIndex = model.OrderIndex,
                IsActive = model.IsActive
            };

            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Menu created successfully." });
        }

        // ---------------------------------------------------------------
        // EDIT
        // ---------------------------------------------------------------
        public async Task<IActionResult> Edit(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null) return NotFound();

            await PopulateViewBag(excludeId: id);
            return View("Form", menu);
        }

        [HttpPost]
        public async Task<IActionResult> EditAjax([FromBody] MenuFormModel model)
        {
            var menu = await _context.Menus.FindAsync(model.Id);
            if (menu == null)
                return NotFound(new { success = false, message = "Menu not found." });

            // Guard: parent tidak boleh diri sendiri atau descendant-nya
            if (model.ParentId.HasValue)
            {
                var descendants = await GetDescendantIdsAsync(model.Id);
                if (model.ParentId == model.Id || descendants.Contains(model.ParentId.Value))
                    return BadRequest(new { success = false, message = "Circular hierarchy detected." });
            }

            menu.Name = model.Name;
            menu.Url = string.IsNullOrWhiteSpace(model.Url) ? null : model.Url;
            menu.Icon = model.Icon;
            menu.ParentId = model.ParentId;
            menu.PermissionCode = string.IsNullOrWhiteSpace(model.PermissionCode) ? null : model.PermissionCode;
            menu.OrderIndex = model.OrderIndex;
            menu.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Menu updated successfully." });
        }

        // ---------------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> DeleteAjax([FromBody] DeleteModel model)
        {
            var menu = await _context.Menus.FindAsync(model.Id);
            if (menu == null)
                return NotFound(new { success = false, message = "Menu not found." });

            var hasChildren = await _context.Menus.AnyAsync(m => m.ParentId == model.Id);
            if (hasChildren)
                return BadRequest(new { success = false, message = "Cannot delete: menu has child items. Delete children first." });

            _context.Menus.Remove(menu);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Menu deleted successfully." });
        }

        // ---------------------------------------------------------------
        // HELPER
        // ---------------------------------------------------------------
        private async Task PopulateViewBag(int? excludeId = null)
        {
            // Untuk dropdown parent — hanya menu yang tidak punya parent (root)
            // agar tidak terlalu dalam hierarkinya
            var query = _context.Menus.AsQueryable();

            if (excludeId.HasValue)
            {
                var descendants = await GetDescendantIdsAsync(excludeId.Value);
                descendants.Add(excludeId.Value);
                query = query.Where(m => !descendants.Contains(m.Id));
            }

            ViewBag.ParentMenus = await query
                .OrderBy(m => m.Name)
                .ToListAsync();

            ViewBag.Permissions = await _context.Permissions
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        private async Task<HashSet<int>> GetDescendantIdsAsync(int menuId)
        {
            var allMenus = await _context.Menus.ToListAsync();
            var result = new HashSet<int>();
            CollectDescendants(menuId, allMenus, result);
            return result;
        }

        private void CollectDescendants(int menuId, List<Menu> allMenus, HashSet<int> result)
        {
            var children = allMenus.Where(m => m.ParentId == menuId);
            foreach (var child in children)
            {
                if (result.Add(child.Id))
                    CollectDescendants(child.Id, allMenus, result);
            }
        }

        // ---------------------------------------------------------------
        // DTO
        // ---------------------------------------------------------------
        public class MenuFormModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string? Url { get; set; }
            public string? Icon { get; set; }
            public int? ParentId { get; set; }
            public string? PermissionCode { get; set; }
            public int OrderIndex { get; set; }
            public bool IsActive { get; set; } = true;
        }

        public class DeleteModel
        {
            public int Id { get; set; }
        }
    }
}