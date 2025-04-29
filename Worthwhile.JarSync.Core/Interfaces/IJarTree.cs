

using Worthwhile.JarSync.Core.Source;

namespace Worthwhile.JarSync.Core.Interfaces
{
    //File
    public interface IJarItemDescriptor
    {
        IJarItemService Service { get; }
        EJarDescriptorAttribute ActiveAttributes { get; set; }
        string FullPath { get; }
        string? Name { get; }
        bool IsReadOnly { get; }
        DateTime LastWriteTime { get; }
        bool Exists { get; }
        string? ParentPath { get; }
        string? ParentName { get; }
        long Size { get; }
        bool ReadAccess { get; }
        bool WriteAccess { get; }
        bool IsRoot { get; }
    }

    //Container such as a folder
    public interface IJarDescriptor : IJarItemDescriptor
    {
        bool Empty { get; }
    }

    public interface IJarTreeService
    {
        IJarService JarService { get; }
        IJarItemService JarItemService { get; }
        IJarSyncOperationMediator SyncMediator { get; }
    }

    public enum EJarDescriptorAttribute
    { 
        None = 0,
        FullPath = 1,
        Name = 2,
        IsReadonly = 4,
        LastWriteTime = 8,
        Exists = 16,
        ParentPath = 32,
        ParentName = 64,
        Size = 128,
        Empty = 256,
        ReadAccess = 512,
        WriteAccess = 1024
    }

    public interface IJarService
    {
        bool JarExists(string path);
        IJarDescriptor CreateJarDescriptor(string parentPath, string name, EJarDescriptorAttribute flags, bool isRoot);
        IJarDescriptor CreateJarDescriptor(string fullPath, EJarDescriptorAttribute flags, bool isRoot);
        void FillAttributes(IJarDescriptor jar, EJarDescriptorAttribute flags);
        IJarDescriptor[] GetJars(IJarDescriptor jar, EJarDescriptorAttribute flags);
        IJarItemDescriptor[] GetJarItems(IJarDescriptor jar, EJarDescriptorAttribute flags);
        void SetReadOnly(IJarDescriptor jar, bool value);
        void CreateJar(IJarDescriptor jar);
        void DeleteJar(IJarDescriptor jar);
    }

    public interface IJarItemService
    {
        IJarItemDescriptor CreateJarItemDescriptor(string parentPath, string name, EJarDescriptorAttribute flags, bool isRoot);
        IJarItemDescriptor CreateJarItemDescriptor(string fullPath, EJarDescriptorAttribute flags, bool isRoot);
        void FillAttributes(IJarItemDescriptor jar, EJarDescriptorAttribute flags);
        void SetReadOnly(IJarItemDescriptor jar, bool value);
        void SetLastWriteTime(IJarItemDescriptor jar, DateTime dateTime);
        bool SupportsNativeSync(IJarItemDescriptor sourceJar, IJarItemDescriptor destinationJar);
        void CreateNewJarItemNative(IJarItemDescriptor sourceJar, IJarItemDescriptor destinationJar);
        void SyncExistingJarItemNative(IJarItemDescriptor sourceJar, IJarItemDescriptor destinationJar);
        void DeleteJarItem(IJarItemDescriptor jar);
    }

    public class JarTargetRequestParams
    {
        public IJarDescriptor SourceJar { get; set; }
        public IJarDescriptor DestinationJar { get; set; }
        public int NumberOfThreads { get; set; }

        public HashSet<string> SkipSubFolders = new HashSet<string>();

        public JarTargetRequestParams(IJarDescriptor aSourceJar, IJarDescriptor aDestinationJar, HashSet<string> aSkipSubFolders, int aNumberOfThreads)
        {
            SourceJar = aSourceJar;
            DestinationJar = aDestinationJar;
            SkipSubFolders = aSkipSubFolders;
            NumberOfThreads = aNumberOfThreads;
        }
    }

    public interface IJarSyncProcessor
    {
        bool Run(JarTargetRequestParams aInput);
    }

    public class JarItemSyncRequestParams
    {
        public ResourceSyncStatus FolderStatus { get; set; }
        public IJarItemDescriptor SourceJarItem { get; set; }
        public IJarItemDescriptor DestinationJarItem { get; set; }

        public JarItemSyncRequestParams(ResourceSyncStatus folderStatus, IJarItemDescriptor sourceJarItem, IJarItemDescriptor destinationJarItem)
        {
            FolderStatus = folderStatus;
            SourceJarItem = sourceJarItem;
            DestinationJarItem = destinationJarItem;
        }
    }

    public interface IJarItemSyncProcessor
    {
        bool SyncJarItem(JarItemSyncRequestParams runParams);
        bool DeleteDestinationJarItem(IJarTreeService treeService, ResourceSyncStatus folderStatus, IJarItemDescriptor destinationJarItem);
    }
}