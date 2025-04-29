
namespace Worthwhile.JarSync.Core.Config
{
    public class JarSyncConfigRoot
    {
        public const string SECTION_NAME = "JarSyncConfigRoot";
        public string NumberOfThreads { get; set; } = "1";
        public ConfigSectionJarSyncStep[] SyncSteps { get; set; } = new ConfigSectionJarSyncStep[] { };
        public int ConcurrentThreads {
            get
            {
                return int.Parse(NumberOfThreads);
            }
        }

        public JarSyncConfigRoot()
        {
        }

        public void Initialize()
        {
            if (ConcurrentThreads < 1 || ConcurrentThreads > 10)
            {
                throw new Exception("Invalid number of threads. NumberOfThreads: [1..10]");
            }

            SyncSteps.ToList().ForEach(s => s.Initialize());
        }
    }
}
