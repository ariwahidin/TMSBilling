//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using TMSBilling.Data;
//using TMSBilling.Models;

//namespace TMSBilling.Repositories
//{
//    public class IntegrationRepository : IIntegrationRepository
//    {
//        private readonly AppDbContext _db;

//        public IntegrationRepository(AppDbContext db)
//        {
//            _db = db;
//        }

//        // ── CRUD ─────────────────────────────────────────────────────────────

//        public async Task<List<Integration>> GetAllAsync()
//            => await _db.Integrations
//                .OrderBy(x => x.Name)
//                .ToListAsync();

//        public async Task<Integration?> GetByIdAsync(int id)
//            => await _db.Integrations.FindAsync(id);

//        public async Task<Integration?> GetByIdWithConnectionAsync(int id)
//            => await _db.Integrations
//                .Include(x => x.Connection)
//                .FirstOrDefaultAsync(x => x.Id == id);

//        public async Task<Integration?> GetByIdFullAsync(int id)
//            => await _db.Integrations
//                .Include(x => x.Connection)
//                .Include(x => x.Recipients)
//                .FirstOrDefaultAsync(x => x.Id == id);

//        public async Task<Integration> CreateAsync(Integration integration)
//        {
//            integration.CreatedAt = DateTime.Now;
//            integration.UpdatedAt = DateTime.Now;
//            _db.Integrations.Add(integration);
//            await _db.SaveChangesAsync();
//            return integration;
//        }

//        public async Task<Integration> UpdateAsync(Integration integration)
//        {
//            integration.UpdatedAt = DateTime.Now;
//            _db.Integrations.Update(integration);
//            await _db.SaveChangesAsync();
//            return integration;
//        }

//        public async Task DeleteAsync(int id)
//        {
//            var entity = await _db.Integrations.FindAsync(id);
//            if (entity != null)
//            {
//                _db.Integrations.Remove(entity);
//                await _db.SaveChangesAsync();
//            }
//        }

//        // ── Dispatcher queries ───────────────────────────────────────────────

//        public async Task<List<Integration>> GetActiveByEventKeyAsync(string eventKey)
//            => await _db.Integrations
//                .Include(x => x.Connection)
//                .Include(x => x.Recipients)
//                .Where(x => x.IsActive && x.EventKey == eventKey && x.Timing == "realtime")
//                .ToListAsync();

//        public async Task<List<Integration>> GetActiveScheduledAsync()
//            => await _db.Integrations
//                .Include(x => x.Connection)
//                .Include(x => x.Recipients)
//                .Where(x => x.IsActive && x.Timing == "scheduled")
//                .ToListAsync();

//        // ── Connection ───────────────────────────────────────────────────────

//        public async Task<IntegrationConnection?> GetConnectionAsync(int integrationId)
//            => await _db.IntegrationConnections
//                .FirstOrDefaultAsync(x => x.IntegrationId == integrationId);

//        public async Task SaveConnectionAsync(IntegrationConnection connection)
//        {
//            var existing = await _db.IntegrationConnections
//                .FirstOrDefaultAsync(x => x.IntegrationId == connection.IntegrationId);

//            if (existing == null)
//            {
//                connection.CreatedAt = DateTime.Now;
//                connection.UpdatedAt = DateTime.Now;
//                _db.IntegrationConnections.Add(connection);
//            }
//            else
//            {
//                connection.Id = existing.Id;
//                connection.CreatedAt = existing.CreatedAt;
//                connection.UpdatedAt = DateTime.Now;
//                _db.Entry(existing).CurrentValues.SetValues(connection);
//            }

//            await _db.SaveChangesAsync();
//        }

//        // ── Recipients ───────────────────────────────────────────────────────

//        public async Task<List<IntegrationRecipient>> GetRecipientsAsync(int integrationId)
//            => await _db.IntegrationRecipients
//                .Where(x => x.IntegrationId == integrationId)
//                .ToListAsync();

//        public async Task SaveRecipientsAsync(int integrationId, List<IntegrationRecipient> recipients)
//        {
//            var existing = _db.IntegrationRecipients.Where(x => x.IntegrationId == integrationId);
//            _db.IntegrationRecipients.RemoveRange(existing);

//            foreach (var r in recipients)
//            {
//                r.Id = 0;
//                r.IntegrationId = integrationId;
//                r.CreatedAt = DateTime.Now;
//                _db.IntegrationRecipients.Add(r);
//            }

//            await _db.SaveChangesAsync();
//        }

//        // ── History ──────────────────────────────────────────────────────────

//        public async Task<IntegrationHistory> AddHistoryAsync(IntegrationHistory history)
//        {
//            history.ExecutedAt = DateTime.Now;
//            _db.IntegrationHistories.Add(history);
//            await _db.SaveChangesAsync();
//            return history;
//        }

//        public async Task<List<IntegrationHistory>> GetHistoriesAsync(int integrationId, int take = 50)
//            => await _db.IntegrationHistories
//                .Where(x => x.IntegrationId == integrationId)
//                .OrderByDescending(x => x.ExecutedAt)
//                .Take(take)
//                .ToListAsync();

//        public async Task<List<IntegrationHistory>> GetRecentHistoriesAsync(int take = 100)
//            => await _db.IntegrationHistories
//                .Include(x => x.Integration)
//                .OrderByDescending(x => x.ExecutedAt)
//                .Take(take)
//                .ToListAsync();

//        // ── Scheduler metadata ───────────────────────────────────────────────

//        public async Task UpdateRunTimesAsync(int integrationId, DateTime lastRun, DateTime? nextRun, string status)
//        {
//            var entity = await _db.Integrations.FindAsync(integrationId);
//            if (entity != null)
//            {
//                entity.LastRunAt = lastRun;
//                entity.NextRunAt = nextRun;
//                entity.LastRunStatus = status;
//                entity.UpdatedAt = DateTime.Now;
//                await _db.SaveChangesAsync();
//            }
//        }
//    }
//}


using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMSBilling.Data;
using TMSBilling.Models;

namespace TMSBilling.Repositories
{
    public class IntegrationRepository : IIntegrationRepository
    {
        private readonly AppDbContext _db;

        public IntegrationRepository(AppDbContext db) => _db = db;

        // ── CRUD ─────────────────────────────────────────────────────────────

        public async Task<List<Integration>> GetAllAsync()
            => await _db.Integrations.OrderBy(x => x.Name).ToListAsync();

        public async Task<Integration?> GetByIdAsync(int id)
            => await _db.Integrations.FindAsync(id);

        public async Task<Integration?> GetByIdWithConnectionAsync(int id)
            => await _db.Integrations
                .Include(x => x.Connection)
                .Include(x => x.Recipients)
                .Include(x => x.Attachments.Where(a => a.IsActive))
                .FirstOrDefaultAsync(x => x.Id == id);

        public async Task<Integration?> GetByIdFullAsync(int id)
            => await _db.Integrations
                .Include(x => x.Connection)
                .Include(x => x.Recipients)
                .Include(x => x.Attachments.OrderBy(a => a.GroupFileKey).ThenBy(a => a.SheetOrder))
                .FirstOrDefaultAsync(x => x.Id == id);

        public async Task<Integration> CreateAsync(Integration integration)
        {
            integration.CreatedAt = DateTime.Now;
            integration.UpdatedAt = DateTime.Now;
            _db.Integrations.Add(integration);
            await _db.SaveChangesAsync();
            return integration;
        }

        public async Task<Integration> UpdateAsync(Integration integration)
        {
            integration.UpdatedAt = DateTime.Now;
            _db.Integrations.Update(integration);
            await _db.SaveChangesAsync();
            return integration;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.Integrations.FindAsync(id);
            if (entity != null) { _db.Integrations.Remove(entity); await _db.SaveChangesAsync(); }
        }

        // ── Dispatcher ───────────────────────────────────────────────────────

        public async Task<List<Integration>> GetActiveByEventKeyAsync(string eventKey)
            => await _db.Integrations
                .Include(x => x.Connection)
                .Include(x => x.Recipients)
                .Include(x => x.Attachments.Where(a => a.IsActive))
                .Where(x => x.IsActive && x.EventKey == eventKey && x.Timing == "realtime")
                .ToListAsync();

        public async Task<List<Integration>> GetActiveScheduledAsync()
            => await _db.Integrations
                .Include(x => x.Connection)
                .Include(x => x.Recipients)
                .Include(x => x.Attachments.Where(a => a.IsActive))
                .Where(x => x.IsActive && x.Timing == "scheduled")
                .ToListAsync();

        // ── Connection ───────────────────────────────────────────────────────

        public async Task<IntegrationConnection?> GetConnectionAsync(int integrationId)
            => await _db.IntegrationConnections.FirstOrDefaultAsync(x => x.IntegrationId == integrationId);

        public async Task SaveConnectionAsync(IntegrationConnection connection)
        {
            var existing = await _db.IntegrationConnections
                .FirstOrDefaultAsync(x => x.IntegrationId == connection.IntegrationId);

            if (existing == null)
            {
                connection.CreatedAt = connection.UpdatedAt = DateTime.Now;
                _db.IntegrationConnections.Add(connection);
            }
            else
            {
                connection.Id = existing.Id;
                connection.CreatedAt = existing.CreatedAt;
                connection.UpdatedAt = DateTime.Now;
                _db.Entry(existing).CurrentValues.SetValues(connection);
            }
            await _db.SaveChangesAsync();
        }

        // ── Recipients ───────────────────────────────────────────────────────

        public async Task<List<IntegrationRecipient>> GetRecipientsAsync(int integrationId)
            => await _db.IntegrationRecipients.Where(x => x.IntegrationId == integrationId).ToListAsync();

        public async Task SaveRecipientsAsync(int integrationId, List<IntegrationRecipient> recipients)
        {
            _db.IntegrationRecipients.RemoveRange(
                _db.IntegrationRecipients.Where(x => x.IntegrationId == integrationId));

            foreach (var r in recipients)
            {
                r.Id = 0;
                r.IntegrationId = integrationId;
                r.CreatedAt = DateTime.Now;
                _db.IntegrationRecipients.Add(r);
            }
            await _db.SaveChangesAsync();
        }

        // ── Attachments ──────────────────────────────────────────────────────

        public async Task<List<IntegrationAttachment>> GetAttachmentsAsync(int integrationId)
            => await _db.IntegrationAttachments
                .Where(x => x.IntegrationId == integrationId)
                .OrderBy(x => x.GroupFileKey)
                .ThenBy(x => x.SheetOrder)
                .ToListAsync();

        public async Task SaveAttachmentsAsync(int integrationId, List<IntegrationAttachment> attachments)
        {
            // Delete semua yang ada, lalu insert ulang
            _db.IntegrationAttachments.RemoveRange(
                _db.IntegrationAttachments.Where(x => x.IntegrationId == integrationId));

            foreach (var a in attachments)
            {
                a.Id = 0;
                a.IntegrationId = integrationId;
                a.CreatedAt = DateTime.Now;
                a.UpdatedAt = DateTime.Now;
                _db.IntegrationAttachments.Add(a);
            }
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAttachmentAsync(int attachmentId)
        {
            var entity = await _db.IntegrationAttachments.FindAsync(attachmentId);
            if (entity != null) { _db.IntegrationAttachments.Remove(entity); await _db.SaveChangesAsync(); }
        }

        // ── History ──────────────────────────────────────────────────────────

        public async Task<IntegrationHistory> AddHistoryAsync(IntegrationHistory history)
        {
            history.ExecutedAt = DateTime.Now;
            _db.IntegrationHistories.Add(history);
            await _db.SaveChangesAsync();
            return history;
        }

        public async Task<List<IntegrationHistory>> GetHistoriesAsync(int integrationId, int take = 50)
            => await _db.IntegrationHistories
                .Where(x => x.IntegrationId == integrationId)
                .OrderByDescending(x => x.ExecutedAt)
                .Take(take)
                .ToListAsync();

        public async Task<List<IntegrationHistory>> GetRecentHistoriesAsync(int take = 100)
            => await _db.IntegrationHistories
                .Include(x => x.Integration)
                .OrderByDescending(x => x.ExecutedAt)
                .Take(take)
                .ToListAsync();

        // ── Scheduler ────────────────────────────────────────────────────────

        public async Task UpdateRunTimesAsync(int integrationId, DateTime lastRun, DateTime? nextRun, string status)
        {
            var entity = await _db.Integrations.FindAsync(integrationId);
            if (entity != null)
            {
                entity.LastRunAt = lastRun;
                entity.NextRunAt = nextRun;
                entity.LastRunStatus = status;
                entity.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<IntegrationHistory?> GetHistoryByIdAsync(int id)
        {
            return await _db.IntegrationHistories.FindAsync(id);
        }
    }
}
