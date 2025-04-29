
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

using Common.EmailService.Interfaces;
using Worthwhile.JarSync.Communication.Azure;
using Worthwhile.JarSync.Core.Source;
using Worthwhile.JarSync.Core.Config;
using Worthwhile.JarSync.Core.Interfaces;

namespace Worthwhile.JarSync.CommonConfiguration
{
    internal static class ServiceInitialization
    {
        public const string ENV_DEV = "dev";
        public const string ENV_PROD = "prod";

        public static IServiceCollection ConfigureFileManagedServices(this IServiceCollection services)
        {
            var env = ENV_PROD;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                env = ENV_DEV;
            }

            Directory.SetCurrentDirectory(GetAppInstallDirectory());

            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile($"appsettings.{env}.json", true, true)
               .AddEnvironmentVariables();
            IConfiguration configuration = builder.Build();

            ConfigureSerilog(configuration, services);

            JarSyncRequestConfig config = BuildConfigurationRequest(configuration, services);
            services.AddSingleton<JarSyncRequestConfig>(config);
            if (config.EmailConfig.IsEnabled)
            {
                services.AddSingleton<IEmailService, EmailService>();
                services.AddSingleton<IEmailProcessor, EmailProcessor>();
                services.AddSingleton<EmailConfig>(config.EmailConfig);
            }

            services.AddScoped<IJarSyncOperationManager, JarSyncOperationManager>();
            services.AddScoped<IJarSyncOperationMediator, JarSyncOperationMediator>();
            services.AddScoped<IEventLogger, ConcurrentEventLogger>();
            services.AddScoped<JarSyncOperationResult>();

            services.AddKeyedScoped<IJarTreeService, Core.Source.WindowsFileSystem.WindowsFileSystemService>(ConfigSectionJarInfo.TS_TYPE_WindowsFileSystem);
            services.AddKeyedScoped<IJarService, Core.Source.WindowsFileSystem.FileSystemFolderService>(ConfigSectionJarInfo.TS_TYPE_WindowsFileSystem);
            services.AddKeyedScoped<IJarItemService, Core.Source.WindowsFileSystem.FileSystemFileService>(ConfigSectionJarInfo.TS_TYPE_WindowsFileSystem);
            return services;
        }

        public static string GetAppInstallDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static JarSyncRequestConfig BuildConfigurationRequest(IConfiguration configuration, IServiceCollection services)
        {
            JarSyncConfigRoot copyConfig = configuration.GetSection(JarSyncConfigRoot.SECTION_NAME).Get<JarSyncConfigRoot>()!;
            copyConfig.Initialize();

            EmailConfig emailConfig = configuration.GetSection(EmailConfig.SECTION_NAME).Get<EmailConfig>()!;
            emailConfig.Initialize();

            SchedulerConfig schedulerConfig = configuration.GetSection(SchedulerConfig.SECTION_NAME).Get<SchedulerConfig>()!;
            schedulerConfig.Initialize();

            JarSyncRequestConfig config = new JarSyncRequestConfig(copyConfig, emailConfig, schedulerConfig);
            return config;
        }

        private static void ConfigureSerilog(IConfiguration config, IServiceCollection services)
        {
            var serilogLogger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .CreateLogger();

            services.AddLogging(builder =>
            {
                builder.AddSerilog(logger: serilogLogger, dispose: true);
            });

            SerilogConfigWriteToTarget[] targets = config.GetSection("Serilog").GetSection("WriteTo").Get<List<SerilogConfigWriteToTarget>>()?.ToArray()!;
        }
    }

    public class SerilogConfigWriteToTarget
    {
        public class TargetArgs
        {
            public string instrumentationKey { get; set; } = "";
            public string path { get; set; } = "";
            public string fileSizeLimitBytes { get; set; } = "";
            public string rollingInterval { get; set; } = "";
            public string source { get; set; } = "";
            public string logName { get; set; } = "";
            public string restrictedToMinimumLevel { get; set; } = "";
        }

        public string Name { get; set; } = "";

        public TargetArgs Args { get; set; } = new TargetArgs();
    }
}
