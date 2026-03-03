using TMSBilling.Models.ViewModels;

namespace TMSBilling.Services
{
    public interface IEmailService
    {
        Task SendSuratPerintahKirimAsync(
            SuratPerintahKirimViewModel model,
            List<string> toEmails,
            List<string>? ccEmails = null,
            int? sentByUserId = null
        );

        Task SendTestEmailAsync(string toEmail);

        Task SendAsync(EmailMessage message);
    }
}