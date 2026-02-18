// Controllers/UserController.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
[RequirePermission("user")]   // semua action di controller ini butuh permission "user"
public class UserController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserController> _logger;

    public UserController(AppDbContext context, ILogger<UserController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: tampilkan halaman index
    public IActionResult Index() => View();

    // GET: form tambah user
    public IActionResult Create() => View("Form", new User());

    // GET: ambil data untuk tabel — semua yang punya permission "user" boleh lihat
    [HttpGet]
    public IActionResult GetUsers()
    {
        var users = _context.Users
            .OrderByDescending(u => u.Id)
            .Select(u => new {
                u.Id,
                u.Username,
                u.UpdatedAt,
                u.UpdatedBy
            }).ToList();

        return Ok(new { success = true, message = "User data found", data = users });
    }

    // POST: create — semua yang punya permission "user" boleh create
    [HttpPost]
    public IActionResult CreateAjax([FromBody] User user)
    {
        _logger.LogInformation("CreateAjax payload: {@User}", user);

        if (_context.Users.Any(u => u.Username == user.Username))
            ModelState.AddModelError("Username", "Username already in use.");

        if (!ModelState.IsValid)
            return BadRequest(new
            {
                success = false,
                message = "Data tidak valid.",
                errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });

        var hasher = new PasswordHasher<User>();
        user.Password = hasher.HashPassword(user, user.Password);
        user.CreatedBy = HttpContext.Session.GetString("username");
        user.CreatedAt = DateTime.Now;
        user.UpdatedBy = HttpContext.Session.GetString("username");
        user.UpdatedAt = DateTime.Now;

        _context.Users.Add(user);
        _context.SaveChanges();

        return Ok(new { success = true, message = "User berhasil ditambahkan.", data = user });
    }

    // GET: form edit
    //public IActionResult Edit(int id)
    //{
    //    var user = _context.Users.Find(id);
    //    if (user == null) return NotFound();
    //    return View("Form", user);
    //}

    public async Task<IActionResult> Edit(int id)
    {
        var user = _context.Users.Find(id);
        if (user == null) return NotFound();

        ViewBag.AllRoles = await _context.Roles.OrderBy(r => r.Name).ToListAsync();
        ViewBag.AssignedRoleIds = await _context.UserRoles
            .Where(ur => ur.UserId == id)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        return View("Form", user);
    }

    // POST: edit
    // DTO baru di UserController
    public class UserEditModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string? Password { get; set; }
        public List<int> RoleIds { get; set; } = new();
    }

    [HttpPost]
    public async Task<IActionResult> EditAjax([FromBody] UserEditModel model)
    {
        var user = _context.Users.Find(model.Id);
        if (user == null)
            return NotFound(new { success = false, message = "User not found." });

        var duplicate = _context.Users.Any(u => u.Username == model.Username && u.Id != model.Id);
        if (duplicate)
            return BadRequest(new { success = false, message = "Username already used by another user." });

        // Update user
        user.Username = model.Username;
        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var hasher = new PasswordHasher<User>();
            user.Password = hasher.HashPassword(user, model.Password);
        }
        user.UpdatedBy = HttpContext.Session.GetString("username") ?? "system";
        user.UpdatedAt = DateTime.Now;

        // Replace UserRoles
        var existingRoles = _context.UserRoles.Where(ur => ur.UserId == model.Id);
        _context.UserRoles.RemoveRange(existingRoles);

        if (model.RoleIds?.Count > 0)
        {
            var newRoles = model.RoleIds.Select(roleId => new UserRole
            {
                UserId = model.Id,
                RoleId = roleId
            });
            await _context.UserRoles.AddRangeAsync(newRoles);
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "User successfully edited." });
    }


    //[HttpPost]
    //public IActionResult EditAjax([FromBody] User model)
    //{
    //    var user = _context.Users.Find(model.Id);
    //    if (user == null)
    //        return NotFound(new { success = false, message = "User not found." });

    //    var duplicate = _context.Users
    //        .Any(u => u.Username == model.Username && u.Id != model.Id);

    //    if (duplicate)
    //        return BadRequest(new { success = false, message = "Username already used by another user." });

    //    user.Username = model.Username;
    //    if (!string.IsNullOrWhiteSpace(model.Password))
    //    {
    //        var hasher = new PasswordHasher<User>();
    //        user.Password = hasher.HashPassword(user, model.Password);
    //    }
    //    user.UpdatedBy = HttpContext.Session.GetString("username") ?? "system";
    //    user.UpdatedAt = DateTime.Now;

    //    _context.SaveChanges();

    //    return Ok(new { success = true, message = "User successfully edited.", data = user });
    //}

    // POST: delete single
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var user = _context.Users.Find(id);
        if (user == null) return NotFound();

        _context.Users.Remove(user);
        _context.SaveChanges();

        return Ok();
    }

    // POST: delete bulk
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteBulk([FromBody] BulkDeleteModel model)
    {
        if (model?.Ids == null || model.Ids.Count == 0)
            return BadRequest(new { message = "No data selected" });

        var users = _context.Users.Where(u => model.Ids.Contains(u.Id)).ToList();
        if (users.Count == 0) return NotFound();

        _context.Users.RemoveRange(users);
        _context.SaveChanges();

        return Ok(new { success = true, message = "Deleted successfully" });
    }

    public class BulkDeleteModel
    {
        public required List<int> Ids { get; set; }
    }
}