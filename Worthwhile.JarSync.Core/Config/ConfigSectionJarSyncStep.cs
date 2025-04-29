
namespace Worthwhile.JarSync.Core.Config
{
    public class ConfigSectionJarSyncStep
    {
        public bool Enabled { get; set; }
        public ConfigSectionJarInfo Source { get; set; } = null!;
        public ConfigSectionJarInfo Destination { get; set; } = null!;
        public string SkipFolders { get; set; } = "";

        public string[] SkipFolderArray { get; set; } = new string[] { };

        public ConfigSectionJarSyncStep()
        {
        }

        public void Initialize()
        {
            SkipFolderArray = string.IsNullOrWhiteSpace(SkipFolders) ? new string[] { } : SkipFolders.Split(",");
        }
    }

    public class ConfigSectionJarInfo
    { 
        public const string TS_TYPE_WindowsFileSystem = "WindowsFileSystem";
        public string FullPath { get; set; } = "";
        public string TreeServiceType { get; set; } = "";
    }
}
