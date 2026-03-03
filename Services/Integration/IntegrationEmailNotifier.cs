using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using TMSBilling.Models;
using TMSBilling.Repositories;

namespace TMSBilling.Services.Integration
{
    public interface IIntegrationEmailNotifier
    {
        Task NotifyAsync(
            TMSBilling.Models.Integration integration,
            SenderResult result,
            Dictionary<string, object?> eventData,
            List<Dictionary<string, object?>>? rows = null,
            List<AttachmentFile>? attachments = null);

        string PreviewHtml(
            TMSBilling.Models.Integration integration,
            SenderResult result,
            Dictionary<string, object?> eventData,
            List<Dictionary<string, object?>>? rows = null);

        List<PlaceholderInfo> GetPlaceholders(
            TMSBilling.Models.Integration integration,
            Dictionary<string, object?>? sampleEventData = null,
            List<Dictionary<string, object?>>? sampleRows = null);
    }

    public class IntegrationEmailNotifier : IIntegrationEmailNotifier
    {
        private readonly IEmailSettingsRepository _emailSettings;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<IntegrationEmailNotifier> _logger;
        private readonly EmailTemplateRenderer _renderer;

        public IntegrationEmailNotifier(
            IEmailSettingsRepository emailSettings,
            IEncryptionService encryption,
            ILogger<IntegrationEmailNotifier> logger)
        {
            _emailSettings = emailSettings;
            _encryption = encryption;
            _logger = logger;
            _renderer = new EmailTemplateRenderer();
        }

        // ── Notify ────────────────────────────────────────────────────────────

        public async Task NotifyAsync(
            TMSBilling.Models.Integration integration,
            SenderResult result,
            Dictionary<string, object?> eventData,
            List<Dictionary<string, object?>>? rows = null,
            List<AttachmentFile>? attachments = null)
        {
            var recipients = integration.Recipients;
            if (recipients == null || !recipients.Any())
            {
                _logger.LogDebug("Integration [{Id}] tidak punya recipient, skip email.", integration.Id);
                return;
            }

            var settings = await _emailSettings.GetActiveSettingsAsync();
            if (settings == null)
            {
                _logger.LogWarning("Email settings tidak tersedia, notification tidak terkirim.");
                return;
            }

            // Determine whether to include inline table based on mode
            var mode = integration.EmailAttachmentMode?.ToLower() ?? "inline";
            var includeInlineTable = integration.EmailIncludeDataTable
                && (mode == "inline" || mode == "both");

            // Build rows untuk inline — gunakan event rows saja kalau mode bukan attachment
            var inlineRows = includeInlineTable ? (rows ?? new()) : new List<Dictionary<string, object?>>();

            var ctx = BuildContext(integration, result, eventData, inlineRows);
            var subject = _renderer.RenderSubject(ctx);
            var htmlBody = _renderer.RenderHtml(ctx);

            var toList  = recipients.Where(r => r.RecipientType == "to").Select(r => r.Email).ToList();
            var ccList  = recipients.Where(r => r.RecipientType == "cc").Select(r => r.Email).ToList();
            var bccList = recipients.Where(r => r.RecipientType == "bcc").Select(r => r.Email).ToList();

            if (!toList.Any()) return;

            try
            {
                var decryptedPassword = _encryption.Decrypt(settings.Password);
                using var smtp = new SmtpClient();

                var secureOption = settings.SmtpPort == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await smtp.ConnectAsync(settings.SmtpHost, settings.SmtpPort, secureOption);
                await smtp.AuthenticateAsync(settings.Username, decryptedPassword);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(settings.DisplayName, settings.FromEmail));
                message.Subject = subject;

                foreach (var to in toList.Where(e => !string.IsNullOrWhiteSpace(e)))
                    message.To.Add(MailboxAddress.Parse(to.Trim()));
                foreach (var cc in ccList.Where(e => !string.IsNullOrWhiteSpace(e)))
                    message.Cc.Add(MailboxAddress.Parse(cc.Trim()));
                foreach (var bcc in bccList.Where(e => !string.IsNullOrWhiteSpace(e)))
                    message.Bcc.Add(MailboxAddress.Parse(bcc.Trim()));

                // ── Build MIME body (multipart/mixed jika ada attachment) ──────
                var hasAttachments = attachments != null && attachments.Any()
                    && (mode == "attachment" || mode == "both");

                if (hasAttachments)
                {
                    var multipart = new Multipart("mixed");

                    // HTML body part
                    var htmlPart = new TextPart("html") { Text = htmlBody };
                    multipart.Add(htmlPart);

                    // Attachment parts
                    foreach (var att in attachments!)
                    {
                        var attPart = new MimePart(att.ContentType)
                        {
                            Content = new MimeContent(new System.IO.MemoryStream(att.Content)),
                            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                            ContentTransferEncoding = ContentEncoding.Base64,
                            FileName = att.FileName
                        };
                        multipart.Add(attPart);
                        _logger.LogInformation("Attaching: {File} ({Size} bytes)", att.FileName, att.Content.Length);
                    }

                    message.Body = multipart;
                }
                else
                {
                    message.Body = new TextPart("html") { Text = htmlBody };
                }

                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                var attCount = hasAttachments ? attachments!.Count : 0;
                _logger.LogInformation(
                    "Email [{Status}] terkirim untuk integration [{Id}] | to={ToCount} | attachments={AttCount}",
                    result.Success ? "OK" : "FAIL", integration.Id, toList.Count, attCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal kirim email integration [{Id}]", integration.Id);
                throw;
            }
        }

        // ── Preview ───────────────────────────────────────────────────────────

        public string PreviewHtml(
            TMSBilling.Models.Integration integration,
            SenderResult result,
            Dictionary<string, object?> eventData,
            List<Dictionary<string, object?>>? rows = null)
        {
            var ctx = BuildContext(integration, result, eventData, rows ?? new());
            return _renderer.RenderHtml(ctx);
        }

        // ── Get Placeholders ──────────────────────────────────────────────────

        public List<PlaceholderInfo> GetPlaceholders(
            TMSBilling.Models.Integration integration,
            Dictionary<string, object?>? sampleEventData = null,
            List<Dictionary<string, object?>>? sampleRows = null)
        {
            var ctx = BuildContext(
                integration,
                new SenderResult { Success = true, Message = "Sample", RowCount = 1, DurationMs = 100 },
                sampleEventData ?? new(),
                sampleRows ?? new());

            return _renderer.GetAvailablePlaceholders(ctx);
        }

        private static EmailRenderContext BuildContext(
            TMSBilling.Models.Integration integration,
            SenderResult result,
            Dictionary<string, object?> eventData,
            List<Dictionary<string, object?>> rows)
        {
            return new EmailRenderContext
            {
                Integration = integration,
                Result = result,
                EventData = eventData,
                Rows = rows
            };
        }
    }
}
