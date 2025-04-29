using Worthwhile.JarSync.Core.Interfaces;

namespace Worthwhile.JarSync.Core.Source
{
    public class JarTreeService(IJarService jarService, IJarItemService jarItemService, IJarSyncOperationMediator mediator) : IJarTreeService
    {
        public IJarService JarService { get => jarService; }
        public IJarItemService JarItemService { get => jarItemService; }
        public IJarSyncOperationMediator SyncMediator { get => mediator; }
    }
}
