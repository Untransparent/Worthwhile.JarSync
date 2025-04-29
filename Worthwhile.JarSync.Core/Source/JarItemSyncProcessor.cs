

using Worthwhile.JarSync.Core.Interfaces;

namespace Worthwhile.JarSync.Core.Source
{
    internal class JarItemSyncProcessor(IJarTreeService sourceJarTreeService, IJarTreeService destinationJarTreeService, IJarSyncOperationMediator syncMediator) 
        : IJarItemSyncProcessor
    {
        protected ResourceSyncStatus _folderStatus = null!;

        public bool SyncJarItem(JarItemSyncRequestParams runParams)
        {
            _folderStatus = runParams.FolderStatus;
            bool success = CheckReadAccess(sourceJarTreeService, runParams.SourceJarItem);
            if (!success) return false;

            success = UpdateLastWriteTime(sourceJarTreeService, runParams.SourceJarItem);
            if (!success) return false;

            destinationJarTreeService.JarItemService.FillAttributes(runParams.DestinationJarItem, EJarDescriptorAttribute.Exists);
            if (!runParams.DestinationJarItem.Exists)
            {
                success = CreateNewFile(runParams.SourceJarItem, runParams.DestinationJarItem);
                return success;
            }

            destinationJarTreeService.JarItemService.FillAttributes(runParams.DestinationJarItem, EJarDescriptorAttribute.LastWriteTime);
            TimeSpan diff = runParams.SourceJarItem.LastWriteTime - runParams.DestinationJarItem.LastWriteTime;
            if (Math.Abs(diff.TotalSeconds) < 10)
            {
                return true;
            }

            success = UpdateExistingFile(runParams.SourceJarItem, runParams.DestinationJarItem);
            return success;
        }

        public bool DeleteDestinationJarItem(IJarTreeService destinationJarTreeService, ResourceSyncStatus folderStatus, IJarItemDescriptor destinationJarItem)
        {
            ResourceMicroStatus fileStatus = syncMediator.StartMicroOperation(folderStatus, destinationJarItem.FullPath, EResourceTargetType.File, EResourceActionType.Delete);
            try
            {
                destinationJarTreeService.JarItemService.DeleteJarItem(destinationJarItem);
                syncMediator.CompleteMicroOperation(fileStatus);
            }
            catch (Exception exc)
            {
                syncMediator.FailMicroOperation(fileStatus, exc);
                return false;
            }
            return true;
        }

        private bool CheckReadAccess(IJarTreeService jarTreeService, IJarItemDescriptor jarItem)
        {
            ResourceMicroStatus status = syncMediator.StartMicroOperation(_folderStatus, jarItem.FullPath, EResourceTargetType.File, EResourceActionType.CheckReadAccess);
            jarTreeService.JarItemService.FillAttributes(jarItem, EJarDescriptorAttribute.ReadAccess);

            if (!jarItem.ReadAccess)
            {
                syncMediator.FailMicroOperation(status, "No read access to file");
                return false;
            }
            syncMediator.CompleteMicroOperation(status);
            return jarItem.ReadAccess;
        }

        private bool CheckWriteAccess(IJarTreeService jarTreeService, IJarItemDescriptor jarItem)
        {
            ResourceMicroStatus status = syncMediator.StartMicroOperation(_folderStatus, jarItem.FullPath, EResourceTargetType.File, EResourceActionType.CheckWriteAccess);
            jarTreeService.JarItemService.FillAttributes(jarItem, EJarDescriptorAttribute.WriteAccess);

            if (!jarItem.WriteAccess)
            {
                syncMediator.FailMicroOperation(status, "No write access to file");
                return false;
            }
            syncMediator.CompleteMicroOperation(status);
            return jarItem.WriteAccess;
        }

        private bool UpdateLastWriteTime(IJarTreeService jarTreeService, IJarItemDescriptor jarItem)
        {
            jarTreeService.JarItemService.FillAttributes(jarItem, EJarDescriptorAttribute.LastWriteTime);
            if (jarItem.LastWriteTime.Year < 1980)
            {
                ResourceMicroStatus status = syncMediator.StartMicroOperation(_folderStatus, jarItem.FullPath, EResourceTargetType.File, EResourceActionType.TimestampUpdate);
                try
                {
                    DateTime lastWriteTime = new DateTime(1980, jarItem.LastWriteTime.Month, jarItem.LastWriteTime.Day);
                    jarTreeService.JarItemService.SetLastWriteTime(jarItem, lastWriteTime);
                    syncMediator.CompleteMicroOperation(status);
                }
                catch (Exception exc)
                {
                    syncMediator.FailMicroOperation(status, exc);
                    return false;
                }
            }
            return true;
        }

        private bool CreateNewFile(IJarItemDescriptor sourceJarItem, IJarItemDescriptor destinationJarItem)
        {
            ResourceMicroStatus fileStatus = syncMediator.StartMicroOperation(_folderStatus, destinationJarItem.FullPath, EResourceTargetType.File, EResourceActionType.Create);
            try
            {
                if (sourceJarTreeService.JarItemService.SupportsNativeSync(sourceJarItem, destinationJarItem))
                {
                    sourceJarTreeService.JarItemService.CreateNewJarItemNative(sourceJarItem, destinationJarItem);
                }
                syncMediator.CompleteMicroOperation(fileStatus);
                return true;
            }
            catch (Exception exc)
            {
                syncMediator.FailMicroOperation(fileStatus, exc);
                return false;
            }
        }

        private bool UpdateExistingFile(IJarItemDescriptor sourceJarItem, IJarItemDescriptor destinationJarItem)
        {
            ResourceMicroStatus fileStatus = syncMediator.StartMicroOperation(_folderStatus, destinationJarItem.FullPath, EResourceTargetType.File, EResourceActionType.Update);
            try
            {
                if (sourceJarTreeService.JarItemService.SupportsNativeSync(sourceJarItem, destinationJarItem))
                {
                    sourceJarTreeService.JarItemService.SyncExistingJarItemNative(sourceJarItem, destinationJarItem);
                }
                syncMediator.CompleteMicroOperation(fileStatus);
                return true;
            }
            catch (Exception exc)
            {
                syncMediator.FailMicroOperation(fileStatus, exc);
                return false;
            }
        }
    }
}