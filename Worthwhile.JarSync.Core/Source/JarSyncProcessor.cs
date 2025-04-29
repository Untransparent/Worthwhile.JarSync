
using Worthwhile.JarSync.Core.Interfaces;

namespace Worthwhile.JarSync.Core.Source
{
    public class JarSyncProcessor(IJarTreeService sourceJarTreeService, IJarTreeService destinationJarTreeService, IJarSyncOperationMediator syncMediator) 
        : IJarSyncProcessor
    {
        private ResourceSyncStatus _folderSyncStatus = null!;

        public bool Run(JarTargetRequestParams aInput)
        {
            _folderSyncStatus = syncMediator.CreateSyncStatus(aInput.SourceJar, aInput.DestinationJar);
            try
            {
                bool status = InternalSync(aInput);
                if (status)
                {
                    syncMediator.CompleteSync(_folderSyncStatus);
                    return true;
                }
                else
                {
                    syncMediator.FailSync(_folderSyncStatus, "One or more errors occurred");
                    return false;
                }
            }
            catch (Exception exc)
            {
                syncMediator.FailSync(_folderSyncStatus, exc);
                return false;
            }
        }

        private bool InternalSync(JarTargetRequestParams aInput)
        {
            bool success = CheckSourceFolder(aInput.SourceJar);
            if (!success)
            {
                return false;
            }

            success = CreateDestinationFolderIfDoesntExist(aInput.DestinationJar);
            if (!success)
            {
                return false;
            }

            success &= SyncFiles(aInput);

            success &= SyncFolders(aInput);

            return success;
        }

        private bool CheckSourceFolder(IJarDescriptor sourceJar)
        {
            if (!sourceJar.IsRoot) return true;

            ResourceMicroStatus status = syncMediator.StartMicroOperation(_folderSyncStatus, sourceJar.FullPath, EResourceTargetType.Directory, EResourceActionType.CheckIfExists);
            if (!sourceJar.Exists)
            {
                syncMediator.FailMicroOperation(status, "Folder does not exist");
                return false;
            }
            syncMediator.CompleteMicroOperation(status);
            status = syncMediator.StartMicroOperation(_folderSyncStatus, sourceJar.FullPath, EResourceTargetType.Directory, EResourceActionType.CheckReadAccess);
            if (!sourceJar.ReadAccess)
            {
                syncMediator.FailMicroOperation(status, "No read access to folder");
                return false;
            }
            syncMediator.CompleteMicroOperation(status);
            return true;
        }

        private bool CheckWriteAccess(IJarTreeService destinationTreeService, IJarDescriptor jar)
        {
            ResourceMicroStatus status = syncMediator.StartMicroOperation(_folderSyncStatus, jar.FullPath, EResourceTargetType.Directory, EResourceActionType.CheckWriteAccess);
            destinationTreeService.JarService.FillAttributes(jar, EJarDescriptorAttribute.WriteAccess);

            if (!jar.WriteAccess)
            {
                syncMediator.FailMicroOperation(status, "No write access to folder");
                return false;
            }
            syncMediator.CompleteMicroOperation(status);
            return jar.WriteAccess;
        }

        private bool CreateDestinationFolderIfDoesntExist(IJarDescriptor jar)
        {
            bool success = true;
            if (jar.IsRoot && jar.Exists)
            { 
                success &= CheckWriteAccess(destinationJarTreeService, jar);
                if (!success) return false;
            }

            if (jar.Exists)
            {
                return true;
            }
            ResourceMicroStatus dirStatus = syncMediator.StartMicroOperation(_folderSyncStatus, jar.FullPath,
                EResourceTargetType.Directory, EResourceActionType.Create);
            try
            {
                destinationJarTreeService.JarService.CreateJar(jar);
                syncMediator.CompleteMicroOperation(dirStatus);
            }
            catch (Exception exc)
            {
                syncMediator.FailMicroOperation(dirStatus, exc);
                return false;
            }
            return true;
        }

        private bool SyncFiles(JarTargetRequestParams aInput)
        {
            bool success = true;
            EJarDescriptorAttribute sourceFlags = EJarDescriptorAttribute.Name;
            IJarItemDescriptor[] sourceFiles = sourceJarTreeService.JarService.GetJarItems(aInput.SourceJar, sourceFlags);
            if (sourceFiles.Length == 0) return true;

            //Build lookup of all source->destination files. Build one processor per file to be run in parallel
            List<JarItemSyncRequestParams> fileSyncRequests = new List<JarItemSyncRequestParams>();
            HashSet<string> sourceFileLookup = new HashSet<string>();
            foreach (IJarItemDescriptor sourceFile in sourceFiles)
            {
                EJarDescriptorAttribute destinationFlags = EJarDescriptorAttribute.Name;
                IJarItemDescriptor destinationJarItem = sourceJarTreeService.JarItemService.CreateJarItemDescriptor(aInput.DestinationJar.FullPath, sourceFile.Name!, destinationFlags, false);
                sourceFileLookup.Add(sourceFile.Name?.ToLower()!);
                JarItemSyncRequestParams request = new JarItemSyncRequestParams(_folderSyncStatus, sourceFile, destinationJarItem);
                fileSyncRequests.Add(request);
            }

            //Delete files in the destination folder which are not present in the source folder
            IJarItemDescriptor[] destinationFiles = destinationJarTreeService.JarService.GetJarItems(aInput.DestinationJar, aInput.DestinationJar.ActiveAttributes);
            foreach (IJarItemDescriptor destinationFile in destinationFiles)
            {
                if (!sourceFileLookup.Contains(destinationFile.Name?.ToLower()!))
                {
                    IJarItemSyncProcessor p = new JarItemSyncProcessor(sourceJarTreeService, destinationJarTreeService, syncMediator);
                    success &= p.DeleteDestinationJarItem(destinationJarTreeService, _folderSyncStatus, destinationFile);
                }
            }

            //Sync files
            fileSyncRequests.AsParallel().WithDegreeOfParallelism(aInput.NumberOfThreads).ForAll(request => {
                IJarItemSyncProcessor p = new JarItemSyncProcessor(sourceJarTreeService, destinationJarTreeService, syncMediator);
                success &= p.SyncJarItem(request);
                if (!success) Console.WriteLine("Error syncing file");
            });

            return success;
        }

        private bool SyncFolders(JarTargetRequestParams aInput)
        {
            bool success = true;
            //Build source folder lookup of sub folders except skipped
            Dictionary<string, IJarDescriptor> sourceFolderLookup = BuildSourceChildJarLookup(aInput.SourceJar, aInput.SkipSubFolders);

            //Delete directories in destination which do not exist in source
            if (aInput.DestinationJar.Exists)
            {
                IJarDescriptor[] jars = destinationJarTreeService.JarService.GetJars(aInput.DestinationJar, aInput.DestinationJar.ActiveAttributes);
                foreach (IJarDescriptor childJar in jars)
                {
                    string folderName = childJar.Name?.ToLower()!;
                    if (!sourceFolderLookup.ContainsKey(folderName))
                    {
                        success &= DeleteDestinationFolder(childJar);
                    }
                }
            }

            //Copy subfolders recursively
            foreach (IJarDescriptor sourceChildJar in sourceFolderLookup.Values)
            {
                EJarDescriptorAttribute destinationFlags = EJarDescriptorAttribute.Name | EJarDescriptorAttribute.Exists;
                IJarDescriptor newChildJar = destinationJarTreeService.JarService.CreateJarDescriptor(aInput.DestinationJar.FullPath, sourceChildJar.Name!, destinationFlags, false);

                JarTargetRequestParams p = new JarTargetRequestParams(sourceChildJar, newChildJar, aInput.SkipSubFolders, aInput.NumberOfThreads);
                IJarSyncProcessor newProcessor = new JarSyncProcessor(sourceJarTreeService, destinationJarTreeService, syncMediator);
                success &= newProcessor.Run(p);
            }
            return success;
        }

        private Dictionary<string, IJarDescriptor> BuildSourceChildJarLookup(IJarDescriptor aParentJar, HashSet<string> aSkipFolders)
        {
            Dictionary<string, IJarDescriptor> sourceFolderLookup = new Dictionary<string, IJarDescriptor>();
            IJarDescriptor[] jars = sourceJarTreeService.JarService.GetJars(aParentJar, aParentJar.ActiveAttributes);
            foreach (IJarDescriptor childJar in jars)
            {
                string folderName = childJar.Name?.ToLower()!;
                if (!aSkipFolders.Contains(folderName))
                {
                    sourceFolderLookup.Add(folderName, childJar);
                }
            }
            return sourceFolderLookup;
        }

        private bool DeleteDestinationFolder(IJarDescriptor jar)
        {
            ResourceMicroStatus deleteStatus = syncMediator.StartMicroOperation(_folderSyncStatus, jar.FullPath, EResourceTargetType.Directory, EResourceActionType.Delete);
            try
            {
                destinationJarTreeService.JarService.DeleteJar(jar);
                syncMediator.CompleteMicroOperation(deleteStatus);
                return true;
            }
            catch (Exception exc)
            {
                syncMediator.FailMicroOperation(deleteStatus, exc);
                return false;
            }
        }
    }
}