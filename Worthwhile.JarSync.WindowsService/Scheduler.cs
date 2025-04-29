using Worthwhile.JarSync.CommonConfiguration;

namespace Worthwhile.JarSync.WindowsService
{
    public class Scheduler : IOnCompleteNotify
    {
        private readonly ILogger<Worker> _logger;
        public bool IsRunning
        {
            get
            {
                if (SyncEngine == null)
                {
                    return false;
                }
                return SyncEngine.IsRunning();
            }
        }

        private ResourceSyncEngineDriver SyncEngine = null!;

        public Scheduler(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public void Run()
        {
            try
            {
                InternalRun();
            }
            catch (Exception aExc)
            {
                string msg = $"Scheduler: the sync engine has failed. Exception: {aExc.ToString()}";
                _logger.LogError(msg);
                SyncEngine.LogMessage(msg);
                SyncEngine.Cancel();
                throw;
            }
        }

        private void InternalRun()
        {
            if (IsRunning)
            {
                return;
            }

            SyncEngine = GetDriver();
            if (!SyncEngine.IsSchedulerEnabled()) {
                string msg = "Scheduler is not enabled. Exiting...";
                _logger.LogError(msg);
                throw new Exception(msg);
            }

            // Ensure the next scheduled time is within the next year and the scheduler is enabled
            DateTime nextScheduledRunTime = SyncEngine.GetNextScheduledRunTime();
            TimeSpan diff = nextScheduledRunTime - DateTime.Now;
            if (diff.TotalMinutes < 0)
            {
                string msg = $"Next scheduled time is in the past: {nextScheduledRunTime.ToString()}";
                _logger.LogError(msg);
                throw new Exception(msg);
            }
            if (diff.TotalDays > 365)
            {
                string msg = $"Next scheduled time is more than a year away: {nextScheduledRunTime.ToString()}";
                _logger.LogError(msg);
                throw new Exception(msg);
            }

            SyncEngine.SetOnCompleteNotify(this);
            Task task = new Task(() => SyncEngine.Run());
            task.Start();
            SyncEngine.LogMessage($"Scheduler: the sync engine is now running. Next execution time is {nextScheduledRunTime.ToString()}");
        }

        public void OnComplete(bool Success)
        {
            SyncEngine = null!;
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            SyncEngine.Cancel();

            SyncEngine = null!;
        }

        private ResourceSyncEngineDriver GetDriver()
        {
            if (SyncEngine == null)
            {
                SyncEngine = new ResourceSyncEngineDriver();
                try
                {
                    SyncEngine.Initialize();
                }
                catch (Exception aExc)
                {
                    Console.WriteLine("Error initializing ResourceSyncEngineDriver. Exiting.");
                    Console.WriteLine(aExc.ToString());
                    throw;
                }
            }
            return SyncEngine;
        }
    }
}
