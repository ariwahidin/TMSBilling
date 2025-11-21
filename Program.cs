using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TMSBilling.Data;
using TMSBilling.Models;
using TMSBilling.Services;

var builder = WebApplication.CreateBuilder(args);



builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
builder.Services.AddHttpClient<ProductController>();
builder.Services.AddSingleton(resolver =>
    resolver.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>().Value);
// daftarkan HttpClient + ApiService
builder.Services.AddHttpClient<ApiService>();

// Add services to the container.
builder.Services.AddControllersWithViews(); 

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddSession();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.MaxAge = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<SelectListService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<SyncronizeWithMcEasy>();

// Daftarkan background worker
builder.Services.AddHostedService<SyncWorker>();


// 🔧 SET DEFAULT CULTURE
var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

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
