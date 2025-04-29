using Worthwhile.JarSync.Core.Interfaces;

namespace Worthwhile.JarSync.Core.Source.WindowsFileSystem
{
    public class FileSystemFolder : JarItemBase, IJarDescriptor
    {
        public IJarItemService Service { get; set; } = null!;
        public bool Empty { get; set; }

        public FileSystemFolder(string fullPath, EJarDescriptorAttribute attributes, bool isRoot) : base(fullPath, attributes, isRoot)
        {
        }
    }
}
