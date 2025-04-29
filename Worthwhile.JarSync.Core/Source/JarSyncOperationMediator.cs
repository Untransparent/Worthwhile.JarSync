
using Worthwhile.JarSync.Core.Interfaces;

namespace Worthwhile.JarSync.Core.Source
{
    public class JarSyncOperationMediator : IJarSyncOperationMediator
    {
        private readonly object _lock = new object();

        private readonly IEventLogger mLogger;
        private readonly JarSyncOperationResult mResult;

        public JarSyncOperationResult Result { get => mResult; }

        public JarSyncOperationMediator(IEventLogger eventLogger, JarSyncOperationResult result)
        {
            mLogger = eventLogger;
            mResult = result;
        }

        public void SendMessage(string msg)
        {
            lock (_lock)
            {
                mLogger.LogInformation(msg);
            }
        }

        public ResourceSyncStatus CreateSyncStatus(IJarItemDescriptor from, IJarItemDescriptor to)
        { 
            lock (_lock)
            {
                return mResult.StartNewSync(from, to);
            }
        }

        public void CompleteSync(ResourceSyncStatus sync)
        {
            string msg;
            lock (_lock)
            {
                mResult.CompleteSync(sync, out msg);
                if (!sync.SyncSource.IsRoot && sync.Success)
                {
                    mLogger.LogDebug(msg);
                }
                else
                {
                    mLogger.LogInformation(msg);
                }
            }
        }

        public void FailSync(ResourceSyncStatus sync, Exception exc)
        {
            string msg;
            lock (_lock)
            {
                mResult.FailSync(sync, exc.Message, out msg);
                mLogger.LogError(msg);
                mLogger.LogError(exc.ToString());
            }
        }

        public void FailSync(ResourceSyncStatus sync, string errorMsg)
        {
            string msg;
            lock (_lock)
            {
                mResult.FailSync(sync, errorMsg, out msg);
                mLogger.LogError(msg);
            }
        }

        public ResourceMicroStatus StartMicroOperation(ResourceSyncStatus sync, string targetPath, EResourceTargetType resourceType, EResourceActionType actionType)
        {
            lock (_lock)
            {
                return sync.AddMicroOperation(sync, targetPath, resourceType, actionType);
            }
        }

        public void CompleteMicroOperation(ResourceMicroStatus operation)
        {
            string msg;
            lock (_lock)
            {
                mResult.CompleteOperation(operation, out msg);
                if (operation.ActionType == EResourceActionType.CheckReadAccess || operation.ActionType == EResourceActionType.CheckWriteAccess)
                {
                    mLogger.LogDebug(msg);
                }
                else
                {
                    mLogger.LogInformation(msg);
                }
            }
        }
        public void FailMicroOperation(ResourceMicroStatus operation, Exception exc)
        {
            string msg;
            lock (_lock)
            {
                mResult.FailOperation(operation, exc.Message, out msg);
                mLogger.LogError(msg);
                mLogger.LogError(exc.ToString());
            }
        }
        public void FailMicroOperation(ResourceMicroStatus operation, string errorMsg)
        {
            string msg;
            lock (_lock)
            {
                mResult.FailOperation(operation, errorMsg, out msg);
                mLogger.LogError(msg);

            }
        }
    }

    public class OperationStatus
    {
        public bool Completed { get; protected set; } = false;
        public bool Success { get; protected set; }
        public string ErrorMessage { get; protected set; } = null!;
        public DateTime StartTime { get; protected set; }
        public DateTime EndTime { get; protected set; }

        public void Complete()
        {
            Completed = true;
            Success = true;
            ErrorMessage = "";
            EndTime = DateTime.Now;
        }

        public void Fail(string errorMessage)
        {
            Completed = true;
            Success = false;
            ErrorMessage = errorMessage;
            EndTime = DateTime.Now;
        }

        public TimeSpan GetDuration()
        {
            return EndTime - StartTime;
        }

        public OperationStatus()
        { 
        }
    }

    public class ResourceSyncStatus : OperationStatus
    {
        public IJarItemDescriptor SyncSource { get; init; } = null!;
        public IJarItemDescriptor SyncDestination { get; init; } = null!;
        private List<ResourceMicroStatus> mResourceStatuses { get; init; } = new List<ResourceMicroStatus>();
        public string InternalKey { get; init; } = null!;

        public static ResourceSyncStatus Start(IJarItemDescriptor from, IJarItemDescriptor to)
        {
            return new ResourceSyncStatus()
                
            {
                SyncSource = from,
                SyncDestination = to,
                Completed = false,
                Success = false,
                ErrorMessage = "",
                StartTime = DateTime.Now,
                InternalKey = $"FROM:{from.FullPath.Trim().ToLower()} .TO:{to.FullPath.Trim().ToLower()}"
            };
        }

        public ResourceMicroStatus AddMicroOperation(ResourceSyncStatus parent, string targetPath, EResourceTargetType resourceType, EResourceActionType actionType)
        {
            ResourceMicroStatus ret = ResourceMicroStatus.Start(parent, targetPath, resourceType, actionType);
            mResourceStatuses.Add(ret);
            return ret;
        }

        public ResourceMicroStatus[] GetMicroStatuses()
        { 
            return mResourceStatuses.ToArray();
        }

        public string GetStatusMessage()
        {
            if (Success)
            {
                string ret = $"JarSynced. Source: {SyncSource.FullPath}, Destination: {SyncDestination.FullPath}, Duration: {GetDuration().TotalSeconds} seconds";
                return ret;
            }
            else
            {
                string ret = $"JarSynced failed!!! Source: {SyncSource.FullPath}, Destination: {SyncDestination.FullPath}, Duration: {GetDuration().TotalSeconds} seconds, Error: {ErrorMessage}";
                return ret;
            }
        }
    }

    public class ResourceMicroStatus : OperationStatus
    {
        public string TargetPath { get; init; } = null!;
        public EResourceTargetType ResourceType { get; init; } //File or Directory
        public EResourceActionType ActionType { get; init; }
        public string InternalKey { get; init; } = null!;
        private ResourceSyncStatus Parent { get; init; } = null!;

        public static ResourceMicroStatus Start(ResourceSyncStatus parent, string targetPath, EResourceTargetType resourceType, EResourceActionType actionType)
        {
            return new ResourceMicroStatus()
            {
                Parent = parent,
                TargetPath = targetPath,
                ResourceType = resourceType,
                ActionType = actionType,
                Completed = false,
                Success = false,
                ErrorMessage = "",
                StartTime = DateTime.Now,
                InternalKey = $"FROM:{targetPath.Trim().ToLower()}.ACTION:{actionType.ToString()}"
            };
        }

        public string GetStatusMessage()
        {
            string action = Success ? $"Action {ResourceType}.{ActionType} completed" : $"Error on action {ResourceType}.{ActionType}. Error: {ErrorMessage}";
            string ret = $"{action}. Target: {TargetPath}, Duration: {GetDuration().TotalSeconds} seconds. ThreadID: {Thread.CurrentThread.ManagedThreadId}";
            return ret;
        }
    }

    public enum EResourceTargetType
    { 
        File,
        Directory
    }

    public enum EResourceActionType
    { 
        CheckIfExists,
        CheckReadAccess,
        CheckWriteAccess,
        Create,
        Update,
        Delete,
        Archive,
        TimestampUpdate
    }
}
