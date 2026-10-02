using k8s;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.StorageProviders.Cassandra;
using LagoVista.IoT.Web.Common.Interfaces.BuildDynamics;
using LagoVista.IoT.Web.Common.Repos.BuildDynamics;
using LagoVista.IoT.Web.Common.BuildDynamics;
using LagoVista.IoT.Web.Common.Models.BuildDynamics;
using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Configuration;
using LagoVista.IoT.Web.Common.Interfaces;
using LagoVista.IoT.Web.Common.Interfaces.Services;
using LagoVista.IoT.Web.Common.Managers;
using LagoVista.IoT.Web.Common.Services;
using LagoVista.IoT.Web.Common.Utils;
using LagoVista.Relational.Storage;
using LagoVista.UserAdmin.Interfaces;
using LagoVista.Web.Common.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Resources;

[assembly: NeutralResourcesLanguage("en")]
namespace LagoVista.IoT.Web.Common
{
    public class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IHostedServiceDiagnosticsManager, HostedServiceDiagnosticsManager>();
            services.AddTransient<IPlatformSmokeTestManager, PlatformSmokeTestManager>();
            services.AddTransient<IMetricsManager, Managers.MetricsManager>();
            services.AddTransient<IMetricsRepo, Repos.MetricsRepos>();
            services.AddTransient<ICacheAborter, CacheAborter>();
            services.AddSingleton<IRuntimeSignedRequestSessionService, RuntimeSignedRequestSessionService>();
            services.AddTransient<ISignedRequestHttpValidator, SignedRequestHttpValidator>();
            services.AddTransient<IEntryIntentService, EntryIntentService>();
            services.AddTransient<IMetricsBySessionRepo, Repos.MetricsBySessionRepo>();
            services.AddTransient<IWorkstreamAuthorityRepository, WorkstreamAuthorityRepository>();
            services.AddTransient<IWorkstreamMessageRepository, WorkstreamMessageRepository>();
            services.AddTransient<IAarCompletionRepository, AarCompletionRepository>();
            services.AddTransient<IStorageRetentionPolicyStore, StorageRetentionPolicyStore>();
            services.AddTransient<IBuildPerformanceTelemetryService, BuildPerformanceTelemetryService>();
            services.AddPostgresMetricsStore();
            services.AddActivityRecordStore<WorkstreamActivityRecord, CassandraActivityRecordStore<WorkstreamActivityRecord>>(
                definition => definition.PartitionBy(record => record.OrganizationId));
            services.AddActivityRecordStore<WorkstreamMessageRecord, CassandraActivityRecordStore<WorkstreamMessageRecord>>(
                definition => definition.PartitionBy(record => record.OrganizationId));
            services.AddActivityRecordStore<BuildExecutionTelemetry, CassandraActivityRecordStore<BuildExecutionTelemetry>>(
                definition => definition.PartitionBy(record => record.OrganizationId));

            services.AddTransient<IMetricsLoggerSettings, MetricsLoggerSettings>();

            services.AddSingleton<IKubernetes>(_ =>
            {
                var config = KubernetesClientConfiguration.IsInCluster()
                    ? KubernetesClientConfiguration.InClusterConfig()
                    : KubernetesClientConfiguration.BuildConfigFromConfigFile();

                return new Kubernetes(config);
            });

            services.AddHttpClient("HostedServiceDiagnosticsCluster", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
            });

            services.AddTransient<IKubernetesPodDiscoveryService, KubernetesPodDiscoveryService>();
            services.AddTransient<IHostedServiceClusterDiagnosticsService, HostedServiceClusterDiagnosticsService>();
            services.AddTransient<IHostedServiceDiagnosticsManager, HostedServiceDiagnosticsManager>();
            services.AddTransient<ILocalHostedServiceDiagnosticsService, LocalHostedServiceDiagnosticsService>();

            services.AddSingleton<IAppConfig, AppConfig>();

        }
    }
}

namespace LagoVista.DependencyInjection
{
    public static class WebCommonModule
    {
        public static void AddWebCommonModule(this IServiceCollection services, IConfigurationRoot configRoot, IAdminLogger logger)
        {
            LagoVista.IoT.Web.Common.Startup.ConfigureServices(services);
        }
    }
}
