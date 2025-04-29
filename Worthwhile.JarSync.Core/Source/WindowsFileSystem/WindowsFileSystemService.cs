using Microsoft.Extensions.DependencyInjection;
using Worthwhile.JarSync.Core.Interfaces;
using Worthwhile.JarSync.Core.Config;

namespace Worthwhile.JarSync.Core.Source.WindowsFileSystem
{
    public class WindowsFileSystemService([FromKeyedServices(ConfigSectionJarInfo.TS_TYPE_WindowsFileSystem)] IJarService jarService,
        [FromKeyedServices(ConfigSectionJarInfo.TS_TYPE_WindowsFileSystem)] IJarItemService jarItemService, 
        IJarSyncOperationMediator mediator) : JarTreeService(jarService, jarItemService, mediator)
    {
    }
}
