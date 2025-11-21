using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TMSBilling.Services
{
    public class SyncWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SyncWorker> _logger;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public SyncWorker(IServiceScopeFactory scopeFactory,
                          ILogger<SyncWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!await _lock.WaitAsync(0, stoppingToken))
                {
                    _logger.LogWarning("Sync masih berjalan, skip interval berikutnya");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sync = scope.ServiceProvider.GetRequiredService<SyncronizeWithMcEasy>();

                    await sync.Run();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker error");
                }
                finally
                {
                    _lock.Release();
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}

