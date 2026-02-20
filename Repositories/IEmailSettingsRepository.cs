using TMSBilling.Models;

namespace TMSBilling.Repositories
{
    public interface IEmailSettingsRepository
    {
        Task<EmailSettings?> GetActiveSettingsAsync();
        Task SaveSettingsAsync(EmailSettings settings);
    }
}