
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Worthwhile.JarSync.Core.Interfaces;
using Worthwhile.JarSync.Core.Source;
using Worthwhile.JarSync.Core.Config;

namespace Worthwhile.JarSync.CommonConfiguration
{
    public interface IOnCompleteNotify
    {
        void OnComplete(bool Success);
    }

    public class ResourceSyncEngineDriver
    {
        private const int WAIT_MILLISECONDS = 1000 * 60 * 1; //1 minute
        private const int WAIT_MILLISECONDS_DEBUG = 1000 * 10; //10 seconds

        private JarSyncRequestConfig mConfig = null!;
        private IJarSyncOperationManager mEngine = null!;
        private IEmailProcessor mEmailProcessor = null!;
        private IEventLogger mLogger = null!;
        private List<DateTime> mNextScheduledRunTimes = null!;
        private bool mIsRunning = false;
        private bool mIsCancellationRequested = false;
        private IOnCompleteNotify onCompleteNotify = null!;

        public void Initialize()
        {
            mIsRunning = false;
            mIsCancellationRequested = false;
            IServiceCollection services = new ServiceCollection();
            services.ConfigureFileManagedServices();
            var app = services.BuildServiceProvider();

            mConfig = app.GetRequiredService<JarSyncRequestConfig>();
            mEngine = app.GetRequiredService<IJarSyncOperationManager>();
            mEngine = app.GetRequiredService<IJarSyncOperationManager>();
            mLogger = app.GetRequiredService<IEventLogger>();

            mEmailProcessor = app.GetService<IEmailProcessor>()!;

            mLogger.LogInformation("ResourceSyncEngineDriver has been initialized");
        }

        public void SetOnCompleteNotify(IOnCompleteNotify notify)
        {
            onCompleteNotify = notify;
        }

        public bool IsRunning()
        {
            return mIsRunning;
        }

        public bool IsSchedulerEnabled()
        {
            return mConfig.SchedulerConfig.IsEnabled;
        }

        public DateTime GetNextScheduledRunTime()
        {
            if (!mConfig.SchedulerConfig.IsEnabled)
            {
                return DateTime.Now.AddSeconds(-1);
            }
            mNextScheduledRunTimes = mConfig.SchedulerConfig.GenerateTimes(DateTime.Now.AddMinutes(1), DateTime.Now.AddYears(1));
            return mNextScheduledRunTimes[0];
        }

        public void Run()
        {
            bool success = false;
            try
            {
                mIsRunning = true;
                if (!WaitIfNeeded()) return;

                Exception? exc = ExecuteEngineSafe(out JarSyncOperationResult result);
                if (exc != null)
                {
                    ReportUnknownSyncErrorSafe(exc);
                    return;
                }
                ReportStatusSafe(result);
                success = result.TotalErrors == 0;
                string status = success ? "successfully" : "with errors";
                mLogger.LogInformation($"ResourceSyncEngineDriver has completed {status} ***********{Environment.NewLine}{Environment.NewLine}");
            }
            finally
            {
                mIsRunning = false;
                if (onCompleteNotify != null)
                {
                    onCompleteNotify.OnComplete(success);
                }
            }
        }

        private bool WaitIfNeeded()
        {
            if (!mConfig.SchedulerConfig.IsEnabled) return true;

            DateTime nextRunTime = GetNextScheduledRunTime();
            if (nextRunTime < DateTime.Now || (nextRunTime - DateTime.Now).TotalDays > 365) return false;

            int waitMilliseconds = System.Diagnostics.Debugger.IsAttached ? WAIT_MILLISECONDS_DEBUG : WAIT_MILLISECONDS;
            mLogger.LogInformation($"Waiting for next scheduled run at {nextRunTime}");

            while (nextRunTime > DateTime.Now)
            {
                Thread.Sleep(waitMilliseconds);
                if (mIsCancellationRequested)
                {
                    return false;
                }
            }
            return true;
        }

        public void LogMessage(string message)
        {
            mLogger.LogInformation(message);
        }

        public void Cancel()
        {
            mIsCancellationRequested = true;
        }

        private Exception? ExecuteEngineSafe(out JarSyncOperationResult result)
        {
            try
            {
                result = mEngine.Run(mConfig);
                return null;
            }
            catch (Exception exc)
            {
                result = null!;
                return exc;
            }
        }

        private void ReportStatusSafe(JarSyncOperationResult result)
        {
            if (mEmailProcessor != null)
            {
                try
                {
                    mLogger.LogInformation("Sending status via email...");
                    mEmailProcessor.SendEmail(result);
                    mLogger.LogInformation("Status email sent");
                }
                catch (Exception exc)
                {
                    mLogger.LogError(exc.ToString());
                }
            }
        }

        private void ReportUnknownSyncErrorSafe(Exception exc)
        {
            try
            {
                mLogger.LogInformation("Sending error status via email...");
                mEmailProcessor.SendEmail(exc);
                mLogger.LogInformation("Status email sent");
            }
            catch (Exception aExc)
            {
                mLogger.LogError(aExc.ToString());
            }
        }
    }
}
