
using Worthwhile.JarSync.Core.Source;

namespace Worthwhile.JarSync.Core.Interfaces
{
    public class JarSyncOperationResult
    {
        public int TotalFilesNew { get; private set; } = 0;
        public int TotalFilesUpdated { get; private set; } = 0;
        public int TotalFilesDeleted { get; private set; } = 0;
        public int TotalFoldersCreated { get; private set; } = 0;
        public int TotalFoldersDeleted { get; private set; } = 0;

        public int TotalErrors { get; private set; } = 0;
        public int TotalTargetsSynced { get; private set; } = 0;

        private List<ResourceSyncStatus> mResourceStatus = new List<ResourceSyncStatus>();
        private object mLock = new object();

        public ResourceSyncStatus StartNewSync(IJarItemDescriptor from, IJarItemDescriptor to)
        { 
            ResourceSyncStatus newSync = ResourceSyncStatus.Start(from, to);

            lock (mLock)
            {
                mResourceStatus.Add(newSync);
            }

            return newSync;
        }

        public void CompleteSync(ResourceSyncStatus sync, out string msg)
        {
            sync.Complete();
            TotalTargetsSynced++;
            msg = sync.GetStatusMessage();
        }

        public void FailSync(ResourceSyncStatus sync, string errorMessage, out string msg)
        {
            sync.Fail(errorMessage);
            TotalErrors++;
            msg = sync.GetStatusMessage();
        }

        public void CompleteOperation(ResourceMicroStatus operation, out string msg)
        {
            operation.Complete();
            if (operation.ResourceType == EResourceTargetType.Directory)
            {
                if (operation.ActionType == EResourceActionType.Create)
                {
                    TotalFoldersCreated++;
                }
                else if (operation.ActionType == EResourceActionType.Delete)
                {
                    TotalFoldersDeleted++;
                }
            }
            else if (operation.ResourceType == EResourceTargetType.File)
            {
                if (operation.ActionType == EResourceActionType.Create)
                {
                    TotalFilesNew++;
                }
                else if (operation.ActionType == EResourceActionType.Update)
                {
                    TotalFilesUpdated++;
                }
                else if (operation.ActionType == EResourceActionType.Delete)
                {
                    TotalFilesDeleted++;
                }
            }
            msg = operation.GetStatusMessage();
        }

        public void FailOperation(ResourceMicroStatus operation, string errorMessage, out string msg)
        {
            operation.Fail(errorMessage);
            TotalErrors++;
            msg = operation.GetStatusMessage();
        }

        public string BuildStatusMessage()
        {
            string ret = $"TotalFoldersCreated: {TotalFoldersCreated}, FoldersDeleted: {TotalFoldersDeleted}, NewFiles: {TotalFilesNew}, " + 
                            $"UpdatedFiles: {TotalFilesUpdated}, DeletedFiles: {TotalFilesDeleted}, TotalErrors: {TotalErrors}, TotalTargetsSynced: {TotalTargetsSynced}";
            return ret;
        }

        public string[] GetErrors()
        {
            List<string> ret = new List<string>();
            foreach (ResourceSyncStatus sync in mResourceStatus)
            {
                if (!sync.Success || !sync.Completed)
                {
                    ret.Add($"Sync Error. From: {sync.SyncSource.FullPath}. To: {sync.SyncDestination.FullPath}. Error: {sync.ErrorMessage}. Completed: {sync.Completed}. StartTime: {sync.StartTime}. Duration: {sync.GetDuration()}");
                }

                ResourceMicroStatus[] statuses = sync.GetMicroStatuses();
                foreach (ResourceMicroStatus status in statuses)
                {
                    if (!status.Success || !status.Completed)
                    {
                        ret.Add($"Operation Error. Target: {status.TargetPath}, Type: {status.ResourceType}, Action: {status.ActionType}. Error: {status.ErrorMessage}. Completed: {status.Completed}. StartTime: {sync.StartTime}. Duration: {sync.GetDuration()}");
                    }
                }
            }
            return ret.ToArray();
        }
    }
}
