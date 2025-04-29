
using Worthwhile.JarSync.Core.Source;

namespace Worthwhile.JarSync.Core.Interfaces
{
    public interface IJarSyncOperationMediator
    {
        JarSyncOperationResult Result { get; }
        void SendMessage(string msg);
        ResourceSyncStatus CreateSyncStatus(IJarItemDescriptor from, IJarItemDescriptor to);
        void CompleteSync(ResourceSyncStatus sync);
        void FailSync(ResourceSyncStatus sync, Exception exc = null!);
        void FailSync(ResourceSyncStatus sync, string errorMsg);
        ResourceMicroStatus StartMicroOperation(ResourceSyncStatus sync, string targetPath, EResourceTargetType resourceType, EResourceActionType actionType);
        void CompleteMicroOperation(ResourceMicroStatus operation);
        void FailMicroOperation(ResourceMicroStatus operation, Exception exc);
        void FailMicroOperation(ResourceMicroStatus operation, string errorMsg);
    }
}