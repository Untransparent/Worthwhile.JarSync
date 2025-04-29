
using Worthwhile.JarSync.Core.Interfaces;

namespace Worthwhile.JarSync.Core.Source.WindowsFileSystem
{    

    public class FileSystemFileService(IJarSyncOperationMediator mediator) : IJarItemService
    {
        public IJarSyncOperationMediator SyncMediator { get => mediator; }

        public IJarItemDescriptor CreateJarItemDescriptor(string parentPath, string name, EJarDescriptorAttribute flags, bool isRoot)
        {
            string fullPath = Path.Combine(parentPath, name);
            return CreateJarItemDescriptor(fullPath, flags, isRoot);
        }

        public IJarItemDescriptor CreateJarItemDescriptor(string fullPath, EJarDescriptorAttribute flags, bool isRoot)
        {
            FileSystemFile descriptor = new FileSystemFile(fullPath, flags, isRoot);
            descriptor.Service = this;
            FillAttributes(descriptor, flags);

            return descriptor;
        }

        public void FillAttributes(IJarItemDescriptor jar, EJarDescriptorAttribute flags)
        {
            FileSystemFile descriptor = (FileSystemFile)jar;
            FileInfo fileInfo = new FileInfo(descriptor.FullPath);
            if (flags.HasFlag(EJarDescriptorAttribute.Name))
            {
                descriptor.Name = Path.GetFileName(descriptor.FullPath);
            }
            if (flags.HasFlag(EJarDescriptorAttribute.IsReadonly))
            { 
                descriptor.IsReadOnly = fileInfo.IsReadOnly;
            }
            if (flags.HasFlag(EJarDescriptorAttribute.Exists))
            {
                descriptor.Exists = fileInfo.Exists;
            }
            if (flags.HasFlag(EJarDescriptorAttribute.LastWriteTime))
            {
                descriptor.LastWriteTime = fileInfo.LastWriteTime;
            }
            if (flags.HasFlag(EJarDescriptorAttribute.ParentPath))
            {
                descriptor.ParentPath = Path.GetDirectoryName(descriptor.FullPath);
            }
            if (flags.HasFlag(EJarDescriptorAttribute.ParentName))
            {
                descriptor.ParentName = Path.GetFileName(Path.GetDirectoryName(descriptor.FullPath));
            }
            if (flags.HasFlag(EJarDescriptorAttribute.Size))
            {
                descriptor.Size = fileInfo.Length;
            }
            if (flags.HasFlag(EJarDescriptorAttribute.ReadAccess))
            {
                try
                {
                    using var stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
                    descriptor.ReadAccess = true;
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    descriptor.ReadAccess = false;
                }
            }
            if (flags.HasFlag(EJarDescriptorAttribute.WriteAccess))
            {
                try
                {
                    using var stream = fileInfo.Open(FileMode.Open, FileAccess.Write, FileShare.None);
                    descriptor.WriteAccess = true;
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    descriptor.WriteAccess = false;
                }
            }
        }

        public void SetReadOnly(IJarItemDescriptor jar, bool value)
        {
            FileInfo fileInfo = new FileInfo(jar.FullPath);
            fileInfo.IsReadOnly = value;
        }

        public void SetLastWriteTime(IJarItemDescriptor jar, DateTime dateTime)
        { 
            FileInfo fileInfo = new FileInfo(jar.FullPath);
            fileInfo.LastWriteTime = dateTime;
        }
        public bool SupportsNativeSync (IJarItemDescriptor sourceJar, IJarItemDescriptor destinationJar)
        {
            return sourceJar.Service is FileSystemFileService && destinationJar.Service is FileSystemFileService;
        }
        public void CreateNewJarItemNative(IJarItemDescriptor sourceJar, IJarItemDescriptor destinationJar)
        {
            File.Copy(sourceJar.FullPath, destinationJar.FullPath, true);
        }
        public void SyncExistingJarItemNative(IJarItemDescriptor sourceJar, IJarItemDescriptor destinationJar)
        {
            FileInfo destinationFile = new FileInfo(destinationJar.FullPath);
            destinationFile.IsReadOnly = false;
            destinationFile.Delete();

            File.Copy(sourceJar.FullPath, destinationJar.FullPath, true);
        }
        public void DeleteJarItem(IJarItemDescriptor jar)
        {
            FileInfo destinationFile = new FileInfo(jar.FullPath);
            destinationFile.IsReadOnly = false;
            destinationFile.Delete();
        }
    }
}
