
using Microsoft.Extensions.DependencyInjection;
using System;
using Worthwhile.JarSync.Core.Config;
using Worthwhile.JarSync.Core.Interfaces;

namespace Worthwhile.JarSync.Core.Source
{
    public class JarSyncOperationManager : IJarSyncOperationManager
    {
        private JarSyncRequestConfig mCopyConfig = null!;
        private IJarSyncOperationMediator mMediator;
        private IServiceProvider mServiceProvider;

        public JarSyncOperationManager(IServiceProvider serviceProvider, IJarSyncOperationMediator mediator)
        {
            mServiceProvider = serviceProvider;
            mMediator = mediator;
            mMediator.SendMessage("JarSyncOperationManager has been initialized");
        }

        public JarSyncOperationResult Run(JarSyncRequestConfig aCopyConfig)
        {
            mCopyConfig = aCopyConfig;
            JarSyncOperationResult ret = mMediator.Result;
            mMediator.SendMessage($"Preparing to sync {mCopyConfig.FileCopyConfig.SyncSteps.Length} folders...");

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            foreach (ConfigSectionJarSyncStep config in mCopyConfig.FileCopyConfig.SyncSteps)
            {
                if (!config.Enabled) continue;

                IJarTreeService sourceService = mServiceProvider.GetRequiredKeyedService<IJarTreeService>(config.Source.TreeServiceType);
                IJarTreeService destinationService = mServiceProvider.GetRequiredKeyedService<IJarTreeService>(config.Destination.TreeServiceType);

                mMediator.SendMessage($"Syncing root {config.Source} to {config.Destination}, Skipping: {config.SkipFolders}");
                HashSet<string> lookup = new HashSet<string>();
                foreach (string skipFolderName in config.SkipFolderArray)
                {
                    lookup.Add(skipFolderName.ToLower());
                }

                EJarDescriptorAttribute sourceFlags = EJarDescriptorAttribute.Name | EJarDescriptorAttribute.Exists | EJarDescriptorAttribute.ReadAccess;
                IJarDescriptor sourceJar = sourceService.JarService.CreateJarDescriptor(config.Source.FullPath, sourceFlags, true);

                EJarDescriptorAttribute destinationFlags = EJarDescriptorAttribute.Name | EJarDescriptorAttribute.Exists | EJarDescriptorAttribute.WriteAccess;
                IJarDescriptor destinationJar = destinationService.JarService.CreateJarDescriptor(config.Destination.FullPath, destinationFlags, true);

                JarSyncProcessor pr = new JarSyncProcessor(sourceService, destinationService, mMediator);
                JarTargetRequestParams p = new JarTargetRequestParams(sourceJar, destinationJar, lookup, mCopyConfig.FileCopyConfig.ConcurrentThreads);
                bool success = pr.Run(p);
                if (success) mMediator.SendMessage($"Root Sync {config.Source} to {config.Destination} has completed{Environment.NewLine}");
                else mMediator.SendMessage($"Root Sync {config.Source} to {config.Destination} has completed with errors{Environment.NewLine}");
            }

            mMediator.SendMessage(ret.BuildStatusMessage());
            string[] errors = ret.GetErrors();
            if (errors.Length > 0)
            {
                mMediator.SendMessage($"The following errors occurred during the file copy operation:");
                foreach (string error in errors)
                {
                    mMediator.SendMessage(error);
                }
            }

            watch.Stop();

            mMediator.SendMessage($"File copy has completed in {watch.Elapsed.Minutes} minutes and {watch.Elapsed.Seconds} seconds **************************{Environment.NewLine}");

            return ret;
        }
    }
}
