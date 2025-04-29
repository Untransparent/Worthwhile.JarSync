using Worthwhile.JarSync.Core.Interfaces;

namespace Worthwhile.JarSync.Core.Source
{
    public abstract class JarItemBase(string fullPath, EJarDescriptorAttribute attributes, bool isRoot)
    {
        public EJarDescriptorAttribute ActiveAttributes { get => attributes; set => attributes = value; }
        public string? Name { get; set; }
        public string FullPath { get => fullPath; }
        public bool IsReadOnly { get; set; }
        public DateTime LastWriteTime { get; set; }
        public bool Exists { get; set; }
        public string? ParentPath { get; set; }
        public string? ParentName { get; set; }
        public long Size { get; set; }
        public bool ReadAccess { get; set; }
        public bool WriteAccess { get; set; }
        public bool IsRoot { get => isRoot; }
    }
}
