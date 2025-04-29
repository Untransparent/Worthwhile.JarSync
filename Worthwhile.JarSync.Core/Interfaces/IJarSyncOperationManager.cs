
using Worthwhile.JarSync.Core.Config;

namespace Worthwhile.JarSync.Core.Interfaces
{
    public interface IJarSyncOperationManager
    {
        JarSyncOperationResult Run(JarSyncRequestConfig aCopyConfig);
    }
}