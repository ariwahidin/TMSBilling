using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSession();
builder.Services.AddScoped<SelectListService>();
// Tambahkan sebelum var app = builder.Build();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

var app = builder.Build();

// 🔧 Auto apply migration
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate(); // Apply semua migration ke DB
        Console.WriteLine("✅ Database migrated successfully at: " + DateTime.Now);

        // ✅ Seed admin user jika belum ada
        if (!db.Users.Any())
        {
            var hasher = new PasswordHasher<User>();
            var admin = new User
            {
                Username = "admin",
                CreatedBy = "System",
                CreatedAt = DateTime.Now,
                UpdatedBy = "System",
                UpdatedAt = DateTime.Now
            };
            admin.Password = hasher.HashPassword(admin, "admin123");
            db.Users.Add(admin);
            db.SaveChanges();
            Console.WriteLine("✅ Admin user created.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database migration failed: {ex.Message}");
        // optional: log to file or stop app if needed
    }
}

app.UseSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
