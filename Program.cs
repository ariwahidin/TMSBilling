using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System.Globalization;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Jobs;
using TMSBilling.Models;
using TMSBilling.Repositories;
using TMSBilling.Services;
using TMSBilling.Services.Integration;
using TMSBilling.Services.Reports;

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


builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPermissionService, PermissionService>();

builder.Services.AddScoped<MenuFilter>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<MenuFilter>();
});

builder.Services.AddScoped<IReportGenerator, ProformaInvoiceReport>();
// builder.Services.AddScoped<IReportGenerator, DeliveryOrderReport>();   ← contoh nanti
// builder.Services.AddScoped<IReportGenerator, SummaryBillingReport>();  ← contoh nanti

builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IEmailSettingsRepository, EmailSettingsRepository>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IEmailService, EmailService>();



// ── HttpClient untuk ApiSender ────────────────────────────────────────────
builder.Services.AddHttpClient("IntegrationHub", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── Integration Hub Repositories ─────────────────────────────────────────
builder.Services.AddScoped<IIntegrationRepository, IntegrationRepository>();

// ── Query Services ────────────────────────────────────────────────────────
builder.Services.AddScoped<IQueryValidator, QueryValidator>();
builder.Services.AddScoped<IQueryExecutor, QueryExecutor>();

// ── File Generator ────────────────────────────────────────────────────────
builder.Services.AddScoped<IFileGenerator, FileGenerator>();

// ── Channel Senders ───────────────────────────────────────────────────────
builder.Services.AddScoped<IChannelSender, GoogleSheetsSender>();
builder.Services.AddScoped<IChannelSender, SftpSender>();
builder.Services.AddScoped<IChannelSender, FtpSender>();
builder.Services.AddScoped<IChannelSender, ApiSender>();
builder.Services.AddScoped<IChannelSender, FileSender>();

// ── Email Notifier ────────────────────────────────────────────────────────
builder.Services.AddScoped<IIntegrationEmailNotifier, IntegrationEmailNotifier>();

// ── Dispatcher ────────────────────────────────────────────────────────────
builder.Services.AddScoped<IIntegrationDispatcher, IntegrationDispatcher>();
builder.Services.AddScoped<IMailReportService, MailReportService>();
builder.Services.AddScoped<IExcelLayoutService, ExcelLayoutService>();

// ── Quartz Scheduler ─────────────────────────────────────────────────────
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    // Quartz menggunakan in-memory store (tidak perlu DB)
    q.UseInMemoryStore();

    // Add to existing Quartz config:
    q.AddJob<MailReportSchedulerJob>(opts => opts.WithIdentity("MailReportJob"));
    q.AddTrigger(opts => opts
        .ForJob("MailReportJob")
        .WithIdentity("MailReportTrigger")
        .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()));
});



builder.Services.AddQuartzHostedService(q =>
{
    q.WaitForJobsToComplete = true;

});

// Register scheduler service sebagai SINGLETON (karena IHostedService lifecycle)
// dan sebagai IIntegrationScheduler untuk injection ke controller
builder.Services.AddSingleton<IntegrationSchedulerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<IntegrationSchedulerService>());
builder.Services.AddSingleton<IIntegrationScheduler>(sp =>
    sp.GetRequiredService<IntegrationSchedulerService>());

builder.Services.AddScoped<IAttachmentBuilder, AttachmentBuilder>();



//   q.AddJob<MailReportSchedulerJob>(opts => opts.WithIdentity("MailReportJob"));
//   q.AddTrigger(opts => opts
//       .ForJob("MailReportJob")
//       .WithIdentity("MailReportTrigger")
//       .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()));


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
