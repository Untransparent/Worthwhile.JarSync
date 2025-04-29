
namespace Worthwhile.JarSync.WindowsService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private Scheduler _scheduler = null!;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _scheduler = new Scheduler(_logger);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                if (!_scheduler.IsRunning)
                {
                    _scheduler.Run();
                }

                await Task.Delay(1000, stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested)
            {
                _scheduler.Stop();
            }
        }
    }    
}
