
namespace Worthwhile.JarSync.Core.Config
{
    public class JarSyncRequestConfig
    {
        public JarSyncConfigRoot FileCopyConfig { get; init; }
        public EmailConfig EmailConfig { get; init; }
        
        public SchedulerConfig SchedulerConfig { get; init; }

        public JarSyncRequestConfig(JarSyncConfigRoot fileCopyConfig, EmailConfig emailConfig, SchedulerConfig schedulerConfig)
        {
            FileCopyConfig = fileCopyConfig;
            EmailConfig = emailConfig;
            SchedulerConfig = schedulerConfig;
        }
    }
}