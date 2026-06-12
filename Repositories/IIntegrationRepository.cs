//using System.Collections.Generic;
//using System.Threading.Tasks;
//using TMSBilling.Models;

//namespace TMSBilling.Repositories
//{
//    public interface IIntegrationRepository
//    {
//        // ── CRUD ─────────────────────────────────────────────────────────────
//        Task<List<Integration>> GetAllAsync();
//        Task<Integration?> GetByIdAsync(int id);
//        Task<Integration?> GetByIdWithConnectionAsync(int id);
//        Task<Integration?> GetByIdFullAsync(int id); // includes Connection + Recipients
//        Task<Integration> CreateAsync(Integration integration);
//        Task<Integration> UpdateAsync(Integration integration);
//        Task DeleteAsync(int id);

//        // ── Dispatcher queries ───────────────────────────────────────────────
//        Task<List<Integration>> GetActiveByEventKeyAsync(string eventKey);
//        Task<List<Integration>> GetActiveScheduledAsync();

//        // ── Connection ───────────────────────────────────────────────────────
//        Task<IntegrationConnection?> GetConnectionAsync(int integrationId);
//        Task SaveConnectionAsync(IntegrationConnection connection);

//        // ── Recipients ───────────────────────────────────────────────────────
//        Task<List<IntegrationRecipient>> GetRecipientsAsync(int integrationId);
//        Task SaveRecipientsAsync(int integrationId, List<IntegrationRecipient> recipients);

//        // ── History ──────────────────────────────────────────────────────────
//        Task<IntegrationHistory> AddHistoryAsync(IntegrationHistory history);
//        Task<List<IntegrationHistory>> GetHistoriesAsync(int integrationId, int take = 50);
//        Task<List<IntegrationHistory>> GetRecentHistoriesAsync(int take = 100);

//        // ── Scheduler metadata ───────────────────────────────────────────────
//        Task UpdateRunTimesAsync(int integrationId, DateTime lastRun, DateTime? nextRun, string status);
//    }
//}


using System.Collections.Generic;
using System.Threading.Tasks;
using TMSBilling.Models;

namespace TMSBilling.Repositories
{
    public interface IIntegrationRepository
    {
        // ── CRUD ─────────────────────────────────────────────────────────────
        Task<List<Integration>> GetAllAsync();
        Task<Integration?> GetByIdAsync(int id);
        Task<Integration?> GetByIdWithConnectionAsync(int id);
        Task<Integration?> GetByIdFullAsync(int id);
        Task<Integration> CreateAsync(Integration integration);
        Task<Integration> UpdateAsync(Integration integration);
        Task DeleteAsync(int id);

        // ── Dispatcher ───────────────────────────────────────────────────────
        Task<List<Integration>> GetActiveByEventKeyAsync(string eventKey);
        Task<List<Integration>> GetActiveScheduledAsync();

        // ── Connection ───────────────────────────────────────────────────────
        Task<IntegrationConnection?> GetConnectionAsync(int integrationId);
        Task SaveConnectionAsync(IntegrationConnection connection);

        // ── Recipients ───────────────────────────────────────────────────────
        Task<List<IntegrationRecipient>> GetRecipientsAsync(int integrationId);
        Task SaveRecipientsAsync(int integrationId, List<IntegrationRecipient> recipients);

        // ── Attachments ──────────────────────────────────────────────────────
        Task<List<IntegrationAttachment>> GetAttachmentsAsync(int integrationId);
        Task SaveAttachmentsAsync(int integrationId, List<IntegrationAttachment> attachments);
        Task DeleteAttachmentAsync(int attachmentId);

        // ── History ──────────────────────────────────────────────────────────
        Task<IntegrationHistory> AddHistoryAsync(IntegrationHistory history);
        Task<List<IntegrationHistory>> GetHistoriesAsync(int integrationId, int take = 50);
        Task<List<IntegrationHistory>> GetRecentHistoriesAsync(int take = 100);

        // ── Scheduler ────────────────────────────────────────────────────────
        Task UpdateRunTimesAsync(int integrationId, DateTime lastRun, DateTime? nextRun, string status);

        Task<IntegrationHistory?> GetHistoryByIdAsync(int id);
    }
}
