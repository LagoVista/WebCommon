using k8s;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Configuration;
using LagoVista.IoT.Web.Common.Interfaces;
using LagoVista.IoT.Web.Common.Interfaces.Services;
using LagoVista.IoT.Web.Common.Managers;
using LagoVista.IoT.Web.Common.Services;
using LagoVista.IoT.Web.Common.Utils;
using LagoVista.UserAdmin.Interfaces;
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
            services.AddTransient<IMetricsManager, Managers.MetricsManager>();
            services.AddTransient<IMetricsRepo, Repos.MetricsRepos>();
            services.AddTransient<ICacheAborter, CacheAborter>();
            services.AddTransient<IEntryIntentService, EntryIntentService>();
            services.AddTransient<IMetricsBySessionRepo, Repos.MetricsBySessionRepo>();

            services.AddTransient<IAppConfig, AppConfig>();
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
