//using System.Net;
//using System.Net.Mail;
using TMSBilling.Models;
using TMSBilling.Models.ViewModels;
using TMSBilling.Repositories;
using TMSBilling.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TMSBilling.Services
{
    public class EmailService : IEmailService
    {
        private readonly IEmailSettingsRepository _emailSettingsRepository;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IEncryptionService _encryptionService;
        private readonly AppDbContext _context;

        public EmailService(
            IEmailSettingsRepository emailSettingsRepository,
            IEmailTemplateService emailTemplateService,
            IEncryptionService encryptionService,
            AppDbContext context)
        {
            _emailSettingsRepository = emailSettingsRepository;
            _emailTemplateService = emailTemplateService;
            _encryptionService = encryptionService;
            _context = context;
        }

        public async Task SendSuratPerintahKirimAsync(
            SuratPerintahKirimViewModel model,
            List<string> toEmails,
            List<string>? ccEmails = null,
            int? sentByUserId = null)
        {
            var settings = await _emailSettingsRepository.GetActiveSettingsAsync()
                ?? throw new InvalidOperationException("Konfigurasi SMTP belum tersedia.");

            // Render template ke HTML string
            var htmlBody = await _emailTemplateService
                .RenderTemplateAsync("SuratPerintahKirim", model);

            var subject = $"Surat Perintah Kirim - {model.NomorOrder}";

            await SendEmailAsync(
                settings,
                toEmails,
                ccEmails,
                subject,
                htmlBody,
                jobId: model.NomorOrder,
                triggerType: sentByUserId.HasValue ? "manual" : "auto",
                triggerStatus: "STARTED",
                sentByUserId: sentByUserId
            );
        }

        public async Task SendTestEmailAsync(string toEmail)
        {
            var settings = await _emailSettingsRepository.GetActiveSettingsAsync()
                ?? throw new InvalidOperationException("Konfigurasi SMTP belum tersedia.");

            var htmlBody = @"
                <div style='font-family:Arial;font-size:13px;'>
                    <p>Ini adalah email percobaan dari <strong>TMS Billing</strong>.</p>
                    <p>Jika Anda menerima email ini, konfigurasi SMTP sudah benar.</p>
                    <br/>
                    <p>Best Regards,<br/><strong>Admin Control Tower</strong></p>
                </div>";

            await SendEmailAsync(
                settings,
                toEmails: new List<string> { toEmail },
                ccEmails: null,
                subject: "Test Email - TMS Billing",
                htmlBody: htmlBody,
                jobId: null,
                triggerType: "manual",
                triggerStatus: null,
                sentByUserId: null
            );
        }

        // ─── Core Send + Log ───────────────────────────────────────────────
        private async Task SendEmailAsync(
            EmailSettings settings,
            List<string> toEmails,
            List<string>? ccEmails,
            string subject,
            string htmlBody,
            string? jobId,
            string triggerType,
            string? triggerStatus,
            int? sentByUserId)
        {
            var log = new EmailLog
            {
                JobId = jobId,
                ToEmail = string.Join(",", toEmails),
                CcEmail = ccEmails != null ? string.Join(",", ccEmails) : null,
                FromEmail = settings.FromEmail,
                Subject = subject,
                TriggerType = triggerType,
                TriggerStatus = triggerStatus,
                SentAt = DateTime.Now,
                IsSuccess = false,
                SentByUserId = sentByUserId
            };

            try
            {
                var decryptedPassword = _encryptionService.Decrypt(settings.Password);

                //using var smtp = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
                using var smtp = new SmtpClient();

                // Port 465 = SSL, Port 587 = STARTTLS
                var secureOption = settings.SmtpPort == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await smtp.ConnectAsync(settings.SmtpHost, settings.SmtpPort, secureOption);
                await smtp.AuthenticateAsync(settings.Username, decryptedPassword);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(settings.DisplayName, settings.FromEmail));

                foreach (var to in toEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
                    message.To.Add(MailboxAddress.Parse(to.Trim()));

                if (ccEmails != null)
                    foreach (var cc in ccEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
                        message.Cc.Add(MailboxAddress.Parse(cc.Trim()));

                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                log.IsSuccess = true;
            }
            catch (Exception ex)
            {
                log.IsSuccess = false;
                log.ErrorMessage = ex.Message;

                // Tetap simpan log meski gagal, jangan throw dulu
            }
            finally
            {
                _context.EmailLogs.Add(log);
                await _context.SaveChangesAsync();
            }

            // Throw setelah log tersimpan supaya caller tahu ada error
            if (!log.IsSuccess)
                throw new InvalidOperationException(log.ErrorMessage);
        }
    }
}