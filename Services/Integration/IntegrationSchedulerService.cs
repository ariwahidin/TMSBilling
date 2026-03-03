using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using TMSBilling.Models;
using TMSBilling.Repositories;
using TMSBilling.Services.Integration;

namespace TMSBilling.Services.Integration
{
    // ──────────────────────────────────────────────────────────────────────────
    // Quartz Job — dijalankan oleh scheduler
    // ──────────────────────────────────────────────────────────────────────────
    public class IntegrationJob : IJob
    {
        private readonly IIntegrationDispatcher _dispatcher;
        private readonly ILogger<IntegrationJob> _logger;

        public IntegrationJob(IIntegrationDispatcher dispatcher, ILogger<IntegrationJob> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var integrationId = context.JobDetail.JobDataMap.GetInt("integrationId");
            _logger.LogInformation("Quartz: Executing integration [{Id}]", integrationId);

            await _dispatcher.DispatchScheduledAsync(integrationId, new Dictionary<string, object?>
            {
                ["scheduled"] = "true",
                ["trigger_time"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IIntegrationScheduler — public interface for controller to call
    // ──────────────────────────────────────────────────────────────────────────
    public interface IIntegrationScheduler
    {
        Task ReloadIntegrationAsync(int integrationId);
        Task RemoveIntegrationAsync(int integrationId);
        Task TriggerNowAsync(int integrationId);
        Task<DateTime?> GetNextRunTimeAsync(int integrationId);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Hosted Service — starts on app boot, manages Quartz scheduler
    // ──────────────────────────────────────────────────────────────────────────
    public class IntegrationSchedulerService : IHostedService, IIntegrationScheduler
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IntegrationSchedulerService> _logger;
        private IScheduler _scheduler = null!;

        public IntegrationSchedulerService(
            ISchedulerFactory schedulerFactory,
            IServiceProvider serviceProvider,
            ILogger<IntegrationSchedulerService> logger)
        {
            _schedulerFactory = schedulerFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            await _scheduler.Start(cancellationToken);

            _logger.LogInformation("Integration Scheduler started.");
            await LoadAllScheduledIntegrationsAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_scheduler != null)
                await _scheduler.Shutdown(cancellationToken);
        }

        private async Task LoadAllScheduledIntegrationsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IIntegrationRepository>();
            var integrations = await repo.GetActiveScheduledAsync();

            foreach (var integration in integrations)
            {
                await ScheduleIntegrationAsync(integration);
            }

            _logger.LogInformation("Loaded {Count} scheduled integrations.", integrations.Count);
        }

        public async Task ReloadIntegrationAsync(int integrationId)
        {
            // Remove existing job if any
            await RemoveIntegrationAsync(integrationId);

            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IIntegrationRepository>();
            var integration = await repo.GetByIdAsync(integrationId);

            if (integration != null && integration.IsActive && integration.Timing == "scheduled")
            {
                await ScheduleIntegrationAsync(integration);
                _logger.LogInformation("Reloaded schedule for integration [{Id}]", integrationId);
            }
        }

        public async Task RemoveIntegrationAsync(int integrationId)
        {
            var jobKey = GetJobKey(integrationId);
            if (await _scheduler.CheckExists(jobKey))
            {
                await _scheduler.DeleteJob(jobKey);
                _logger.LogInformation("Removed schedule for integration [{Id}]", integrationId);
            }
        }

        public async Task TriggerNowAsync(int integrationId)
        {
            var jobKey = GetJobKey(integrationId);
            if (await _scheduler.CheckExists(jobKey))
            {
                await _scheduler.TriggerJob(jobKey);
                _logger.LogInformation("Manually triggered integration [{Id}]", integrationId);
            }
            else
            {
                // Create a one-time job for manual trigger
                var job = CreateJob(integrationId);
                var trigger = TriggerBuilder.Create()
                    .ForJob(job)
                    .StartNow()
                    .Build();

                await _scheduler.ScheduleJob(job, trigger);
            }
        }

        public async Task<DateTime?> GetNextRunTimeAsync(int integrationId)
        {
            var triggerKey = new TriggerKey($"trigger_{integrationId}", "integration");
            var trigger = await _scheduler.GetTrigger(triggerKey);
            return trigger?.GetNextFireTimeUtc()?.LocalDateTime;
        }

        private async Task ScheduleIntegrationAsync(TMSBilling.Models.Integration integration)
        {
            var cron = BuildCronExpression(integration);
            if (cron == null)
            {
                _logger.LogWarning("Integration [{Id}] tidak memiliki schedule config yang valid. Skip.", integration.Id);
                return;
            }

            var job = CreateJob(integration.Id);
            var trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger_{integration.Id}", "integration")
                .WithCronSchedule(cron)
                .Build();

            await _scheduler.ScheduleJob(job, trigger);

            var nextRun = trigger.GetNextFireTimeUtc()?.LocalDateTime;
            _logger.LogInformation("Scheduled [{Id}] '{Name}' → cron: {Cron} | next: {Next}",
                integration.Id, integration.Name, cron, nextRun);

            // Update NextRunAt in DB
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IIntegrationRepository>();
            await repo.UpdateRunTimesAsync(integration.Id, integration.LastRunAt ?? DateTime.Now, nextRun, integration.LastRunStatus);
        }

        private IJobDetail CreateJob(int integrationId)
        {
            return JobBuilder.Create<IntegrationJob>()
                .WithIdentity($"job_{integrationId}", "integration")
                .UsingJobData("integrationId", integrationId)
                .StoreDurably()
                .Build();
        }

        private JobKey GetJobKey(int integrationId) =>
            new JobKey($"job_{integrationId}", "integration");

        /// <summary>
        /// Build Quartz cron expression from Integration schedule config.
        /// Cron format: "second minute hour dayOfMonth month dayOfWeek"
        /// </summary>
        private string? BuildCronExpression(TMSBilling.Models.Integration integration)
        {
            int hour = integration.ScheduleHour ?? 0;
            int minute = integration.ScheduleMinute ?? 0;

            return integration.ScheduleFreq?.ToLower() switch
            {
                // Daily: fire every day at HH:MM
                "daily" => $"0 {minute} {hour} * * ?",

                // Weekly: fire on specific day of week
                // Quartz DayOfWeek: 1=SUN, 2=MON, ..., 7=SAT
                "weekly" when integration.ScheduleDayOfWeek.HasValue
                    => $"0 {minute} {hour} ? * {(integration.ScheduleDayOfWeek.Value + 1)}",

                // Monthly: fire on specific day of month
                "monthly" when integration.ScheduleDayOfMonth.HasValue
                    => $"0 {minute} {hour} {integration.ScheduleDayOfMonth} * ?",

                _ => null
            };
        }
    }
}
