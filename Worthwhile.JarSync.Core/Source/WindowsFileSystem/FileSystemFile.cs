using Worthwhile.JarSync.Core.Interfaces;

namespace Worthwhile.JarSync.Core.Source.WindowsFileSystem
{
    public class FileSystemFile : JarItemBase, IJarItemDescriptor
    {
        public IJarItemService Service { get; set; } = null!;

        public FileSystemFile(string fullPath, EJarDescriptorAttribute attributes, bool isRoot) : base(fullPath, attributes, isRoot)
        {
        }
    }
}
