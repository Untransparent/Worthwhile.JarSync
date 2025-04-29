
using Microsoft.Extensions.DependencyInjection;
using Worthwhile.JarSync.Core.Config;
using Worthwhile.JarSync.Core.Interfaces;

namespace Worthwhile.JarSync.Core.Source.WindowsFileSystem
{
    public class FileSystemFolderService([FromKeyedServices(ConfigSectionJarInfo.TS_TYPE_WindowsFileSystem)] IJarItemService jarItemService, 
        IJarSyncOperationMediator mediator) : IJarService
    {
        public IJarItemService JarItemService { get => jarItemService; }
        public IJarSyncOperationMediator SyncMediator { get => mediator; }

        public bool JarExists(string path)
        { 
            return Directory.Exists(path);
        }

        public IJarDescriptor CreateJarDescriptor(string parentPath, string name, EJarDescriptorAttribute flags, bool isRoot)
        {
            string fullPath = Path.Combine(parentPath, name);
            return CreateJarDescriptor(fullPath, flags, isRoot);
        }

        public IJarDescriptor CreateJarDescriptor(string fullPath, EJarDescriptorAttribute flags, bool isRoot)
        {
            FileSystemFolder descriptor = new FileSystemFolder(fullPath, flags, isRoot);
            descriptor.Service = JarItemService;
            FillAttributes(descriptor, flags);

            return descriptor;
        }

        public void FillAttributes(IJarDescriptor jar, EJarDescriptorAttribute flags)
        {
            FileSystemFolder descriptor = (FileSystemFolder)jar;
            DirectoryInfo dirInfo = new DirectoryInfo(descriptor.FullPath);
            if (flags.HasFlag(EJarDescriptorAttribute.Name))
            {
                descriptor.Name = Path.GetFileName(descriptor.FullPath);
            }
            if (flags.HasFlag(EJarDescriptorAttribute.Exists))
            {
                descriptor.Exists = Directory.Exists(descriptor.FullPath);
            }
            if (flags.HasFlag(EJarDescriptorAttribute.LastWriteTime))
            {
                descriptor.LastWriteTime = Directory.GetLastWriteTime(descriptor.FullPath);
            }
            if (flags.HasFlag(EJarDescriptorAttribute.ParentPath))
            {
                descriptor.ParentPath = Path.GetDirectoryName(descriptor.FullPath);
            }
            if (flags.HasFlag(EJarDescriptorAttribute.ParentName))
            {
                descriptor.ParentName = Path.GetFileName(Path.GetDirectoryName(descriptor.FullPath));
            }
            if (flags.HasFlag(EJarDescriptorAttribute.IsReadonly))
            {
                descriptor.IsReadOnly = dirInfo.Attributes.HasFlag(FileAttributes.ReadOnly);
            }
            if (flags.HasFlag(EJarDescriptorAttribute.ReadAccess))
            {
                string[] dirs = [], files = [];
                try
                {
                    dirs = Directory.GetDirectories(jar.FullPath, "abc987", SearchOption.TopDirectoryOnly);
                    files = Directory.GetFiles(jar.FullPath, "abc987", SearchOption.TopDirectoryOnly);
                }
                catch {
                    descriptor.ReadAccess = false;
                }
                descriptor.ReadAccess = dirs.Length + files.Length >= 0;
            }
            if (flags.HasFlag(EJarDescriptorAttribute.WriteAccess))
            {
                string tempFilePath = Path.Combine(jar.FullPath, Path.GetTempFileName());
                try
                {
                    File.Create(tempFilePath + "testtempcreate.txt").Close();
                    File.Delete(tempFilePath + "testtempcreate.txt");
                }
                catch
                {
                    descriptor.WriteAccess = false;
                }
                descriptor.WriteAccess = true;
            }
        }
        public IJarDescriptor[] GetJars(IJarDescriptor jar, EJarDescriptorAttribute flags)
        {
            string[] dirs = Directory.GetDirectories(jar.FullPath);
            List<IJarDescriptor> ret = new List<IJarDescriptor>();
            foreach (string dir in dirs)
            {
                IJarDescriptor newJar = CreateJarDescriptor(dir, flags, false);
                ret.Add(newJar);            
            }
            return ret.ToArray();
        }
        public IJarItemDescriptor[] GetJarItems(IJarDescriptor jar, EJarDescriptorAttribute flags)
        {
            string[] files = Directory.GetFiles(jar.FullPath);
            List<IJarItemDescriptor> ret = new List<IJarItemDescriptor>();
            foreach (string filePath in files)
            {
                IJarItemDescriptor newJarItem = jarItemService.CreateJarItemDescriptor(filePath, flags, false);
                ret.Add(newJarItem);
            }
            return ret.ToArray();
        }
        public string GetJarName(IJarDescriptor jar)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(jar.FullPath);
            string dirName = dirInfo.Name;
            return dirName;
        }
        public bool IsReadOnly(IJarDescriptor jar)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(jar.FullPath);
            return directoryInfo.Attributes.HasFlag(FileAttributes.ReadOnly);
        }
        public void SetReadOnly(IJarDescriptor jar, bool value)
        { 
        }
        private void ClearReadOnlyFolderStructure(DirectoryInfo dir)
        {
            if (dir == null)
                return;

            dir.Attributes = FileAttributes.Normal;
            foreach (FileInfo fi in dir.GetFiles())
            {
                if (fi.IsReadOnly)
                {
                    SyncMediator.SendMessage($"Clearing readonly attribute for {fi.FullName}");
                    fi.Attributes = FileAttributes.Normal;
                }
            }
            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                ClearReadOnlyFolderStructure(di);
            }
        }
        public void CreateJar(IJarDescriptor jar)
        { 
            DirectoryInfo directoryInfo = new DirectoryInfo(jar.FullPath);
            directoryInfo.Create();
            FillAttributes(jar, EJarDescriptorAttribute.Exists);
        }
        public void DeleteJar(IJarDescriptor jar)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(jar.FullPath);
            ClearReadOnlyFolderStructure(directoryInfo);
            directoryInfo.Delete(true);
        }
    }    
}
