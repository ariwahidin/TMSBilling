using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Threading.Tasks;
using TMSBilling.Services;

namespace TMSBilling.Jobs
{
    // ──────────────────────────────────────────────────────────────
    // Quartz.NET Job — runs every 5 minutes, checks due schedules
    // ──────────────────────────────────────────────────────────────
    [DisallowConcurrentExecution]
    public class MailReportSchedulerJob : IJob
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<MailReportSchedulerJob> _logger;

        public MailReportSchedulerJob(IServiceProvider sp, ILogger<MailReportSchedulerJob> logger)
        {
            _sp = sp;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IMailReportService>();
                await svc.ProcessScheduledAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MailReportSchedulerJob failed");
            }
        }
    }
}

// ══════════════════════════════════════════════════════════════
// Tambahkan konfigurasi berikut di Program.cs / Startup.cs
// ══════════════════════════════════════════════════════════════

/*
// Di bagian services (setelah services.AddQuartz(...) yang sudah ada):

services.AddScoped<IMailReportService, MailReportService>();

// Tambahkan job ke Quartz scheduler yang sudah ada:
q.AddJob<MailReportSchedulerJob>(opts => opts.WithIdentity("MailReportJob"));
q.AddTrigger(opts => opts
    .ForJob("MailReportJob")
    .WithIdentity("MailReportTrigger")
    .WithSimpleSchedule(x => x
        .WithIntervalInMinutes(5)
        .RepeatForever())
);
*/