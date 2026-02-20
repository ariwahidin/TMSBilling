using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Models.ViewModels;
using TMSBilling.Repositories;
using TMSBilling.Services;

namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class EmailSettingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailSettingsRepository _emailSettingsRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly IEmailService _emailService;

        public EmailSettingsController(
            AppDbContext context,
            IEmailSettingsRepository emailSettingsRepository,
            IEncryptionService encryptionService,
            IEmailService emailService)
        {
            _context = context;
            _emailSettingsRepository = emailSettingsRepository;
            _encryptionService = encryptionService;
            _emailService = emailService;
        }

        // GET: /EmailSettings
        public async Task<IActionResult> Index()
        {
            var settings = await _emailSettingsRepository.GetActiveSettingsAsync();

            var viewModel = new EmailSettingsViewModel();

            if (settings != null)
            {
                viewModel.SmtpHost = settings.SmtpHost;
                viewModel.SmtpPort = settings.SmtpPort;
                viewModel.FromEmail = settings.FromEmail;
                viewModel.DisplayName = settings.DisplayName;
                viewModel.Username = settings.Username;
                viewModel.Password = null; // jangan tampilkan password
                viewModel.UseSSL = settings.UseSSL;
                viewModel.IsActive = settings.IsActive;
            }

            return View(viewModel);
        }

        // POST: /EmailSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(EmailSettingsViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            var existing = await _emailSettingsRepository.GetActiveSettingsAsync();

            // Jika password dikosongkan saat edit, pakai password lama
            string encryptedPassword;
            if (string.IsNullOrEmpty(viewModel.Password))
            {
                encryptedPassword = existing?.Password ?? string.Empty;
            }
            else
            {
                encryptedPassword = _encryptionService.Encrypt(viewModel.Password);
            }

            var settings = new EmailSettings
            {
                SmtpHost = viewModel.SmtpHost,
                SmtpPort = viewModel.SmtpPort,
                FromEmail = viewModel.FromEmail,
                DisplayName = viewModel.DisplayName,
                Username = viewModel.Username,
                Password = encryptedPassword,
                UseSSL = viewModel.UseSSL,
                IsActive = viewModel.IsActive,
                UpdatedBy = User.Identity?.Name ?? "system",
                UpdatedAt = DateTime.Now
            };

            await _emailSettingsRepository.SaveSettingsAsync(settings);

            TempData["Success"] = "Konfigurasi email berhasil disimpan.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /EmailSettings/TestEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestEmail(string testEmailAddress)
        {
            if (string.IsNullOrEmpty(testEmailAddress))
            {
                TempData["Error"] = "Email tujuan test tidak boleh kosong.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _emailService.SendTestEmailAsync(testEmailAddress);
                TempData["Success"] = $"Test email berhasil dikirim ke {testEmailAddress}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Gagal mengirim email: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> TestEmail(string testEmailAddress)
        //{
        //    if (string.IsNullOrEmpty(testEmailAddress))
        //    {
        //        TempData["Error"] = "Email tujuan test tidak boleh kosong.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    var settings = await _emailSettingsRepository.GetActiveSettingsAsync();
        //    if (settings == null)
        //    {
        //        TempData["Error"] = "Konfigurasi SMTP belum tersedia.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    // Akan diimplementasi setelah EmailService selesai
        //    TempData["Success"] = $"Test email akan dikirim ke {testEmailAddress}.";
        //    return RedirectToAction(nameof(Index));
        //}
    }
}