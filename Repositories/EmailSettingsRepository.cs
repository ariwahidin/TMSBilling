using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Models;

namespace TMSBilling.Repositories
{
    public class EmailSettingsRepository : IEmailSettingsRepository
    {
        private readonly AppDbContext _context;

        public EmailSettingsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<EmailSettings?> GetActiveSettingsAsync()
        {
            return await _context.EmailSettings
                .Where(x => x.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task SaveSettingsAsync(EmailSettings settings)
        {
            var existing = await _context.EmailSettings.FirstOrDefaultAsync();

            if (existing == null)
            {
                _context.EmailSettings.Add(settings);
            }
            else
            {
                existing.SmtpHost = settings.SmtpHost;
                existing.SmtpPort = settings.SmtpPort;
                existing.FromEmail = settings.FromEmail;
                existing.DisplayName = settings.DisplayName;
                existing.Username = settings.Username;
                existing.Password = settings.Password;
                existing.UseSSL = settings.UseSSL;
                existing.IsActive = settings.IsActive;
                existing.UpdatedBy = settings.UpdatedBy;
                existing.UpdatedAt = DateTime.Now;

                _context.EmailSettings.Update(existing);
            }

            await _context.SaveChangesAsync();
        }
    }
}