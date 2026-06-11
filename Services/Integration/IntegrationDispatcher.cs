using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TMSBilling.Models;
using TMSBilling.Repositories;

namespace TMSBilling.Services.Integration
{
    public interface IIntegrationDispatcher
    {
        Task DispatchAsync(string eventKey, Dictionary<string, object?> eventData);
        Task DispatchScheduledAsync(int integrationId, Dictionary<string, object?> eventData);
    }

    public class IntegrationDispatcher : IIntegrationDispatcher
    {
        private readonly IIntegrationRepository _repo;
        private readonly IEnumerable<IChannelSender> _senders;
        private readonly IQueryExecutor _queryExecutor;
        private readonly IIntegrationEmailNotifier _emailNotifier;
        private readonly IAttachmentBuilder _attachmentBuilder;
        private readonly ILogger<IntegrationDispatcher> _logger;

        public IntegrationDispatcher(
            IIntegrationRepository repo,
            IEnumerable<IChannelSender> senders,
            IQueryExecutor queryExecutor,
            IIntegrationEmailNotifier emailNotifier,
            IAttachmentBuilder attachmentBuilder,
            ILogger<IntegrationDispatcher> logger)
        {
            _repo = repo;
            _senders = senders;
            _queryExecutor = queryExecutor;
            _emailNotifier = emailNotifier;
            _attachmentBuilder = attachmentBuilder;
            _logger = logger;
        }

        public async Task DispatchAsync(string eventKey, Dictionary<string, object?> eventData)
        {
            var integrations = await _repo.GetActiveByEventKeyAsync(eventKey);
            if (integrations.Count == 0) { _logger.LogDebug("No active integrations for: {Key}", eventKey); return; }

            _logger.LogInformation("Dispatching '{Key}' to {Count} integration(s)", eventKey, integrations.Count);
            foreach (var integration in integrations)
                await RunIntegrationAsync(integration, eventData, "realtime");
        }

        public async Task DispatchScheduledAsync(int integrationId, Dictionary<string, object?> eventData)
        {
            var integration = await _repo.GetByIdWithConnectionAsync(integrationId);
            if (integration == null) { _logger.LogWarning("Integration [{Id}] not found", integrationId); return; }

            eventData["scheduled"] = "true";
            await RunIntegrationAsync(integration, eventData, "scheduled");
        }

        private async Task RunIntegrationAsync(
            TMSBilling.Models.Integration integration,
            Dictionary<string, object?> eventData,
            string triggerType)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Running [{Id}] '{Name}' via {Channel}", integration.Id, integration.Name, integration.ChannelType);

            var history = new IntegrationHistory
            {
                IntegrationId = integration.Id,
                EventKey = integration.EventKey,
                TriggerType = triggerType,
                EventDataJson = JsonSerializer.Serialize(eventData)
            };

            SenderResult result;
            List<Dictionary<string, object?>> rows = new();

            try
            {
                if (integration.Connection == null)
                    throw new InvalidOperationException("Konfigurasi koneksi belum diisi.");

                // ── Build rows dari source ────────────────────────────────────
                if (integration.SourceType == "query" && !string.IsNullOrWhiteSpace(integration.Query))
                {
                    rows = await _queryExecutor.ExecuteAsync(integration.Query, eventData);
                    _logger.LogInformation("Query returned {Count} rows", rows.Count);
                }
                else
                {
                    rows = new List<Dictionary<string, object?>> { eventData };
                }

                // ── Find & run sender ─────────────────────────────────────────
                var sender = FindSender(integration.ChannelType)
                    ?? throw new InvalidOperationException($"Sender '{integration.ChannelType}' tidak ditemukan.");

                result = await sender.SendAsync(new SenderContext
                {
                    Integration = integration,
                    Connection = integration.Connection,
                    Rows = rows,
                    EventData = eventData,
                    IsScheduled = triggerType == "scheduled"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Integration [{Id}] error", integration.Id);
                result = SenderResult.Fail(ex.Message, ex.ToString());
            }

            sw.Stop();
            result.DurationMs = sw.ElapsedMilliseconds;

            history.Status = result.Success ? "success" : "failed";
            history.Message = result.Message;
            history.ErrorDetail = result.ErrorDetail;
            history.ResponseBody = result.ResponseBody;     // ← tambah
            history.RequestPayload = result.RequestPayload; // ← tambah
            history.RowCount = result.RowCount;
            history.DurationMs = result.DurationMs;

            // ── Email notification ────────────────────────────────────────────
            try
            {
                bool shouldEmail = (result.Success && integration.SendEmailOnSuccess)
                                || (!result.Success && integration.SendEmailOnFailure);

                if (shouldEmail)
                {
                    // Build attachments (hanya jika mode attachment/both)
                    List<AttachmentFile>? attachments = null;
                    var mode = integration.EmailAttachmentMode?.ToLower() ?? "inline";

                    if ((mode == "attachment" || mode == "both") && integration.Attachments.Any())
                    {
                        try
                        {
                            attachments = await _attachmentBuilder.BuildAsync(integration, eventData);
                            _logger.LogInformation("Built {Count} attachment(s)", attachments.Count);
                        }
                        catch (Exception attEx)
                        {
                            _logger.LogWarning(attEx, "Gagal build attachments, email tetap dikirim tanpa attachment.");
                        }
                    }

                    await _emailNotifier.NotifyAsync(integration, result, eventData, rows, attachments);
                    history.EmailSent = true;
                }
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(emailEx, "Email gagal untuk integration [{Id}]", integration.Id);
                history.EmailError = emailEx.Message;
            }

            if (triggerType == "scheduled")
                await _repo.UpdateRunTimesAsync(integration.Id, DateTime.Now, null, history.Status);

            await _repo.AddHistoryAsync(history);
            _logger.LogInformation("Integration [{Id}] {Status} in {Ms}ms", integration.Id, history.Status, result.DurationMs);
        }

        private IChannelSender? FindSender(string channelType)
        {
            foreach (var s in _senders)
                if (s.ChannelType.Equals(channelType, StringComparison.OrdinalIgnoreCase)) return s;
            return null;
        }
    }
}
