using LagoVista.CloudStorage.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Utils;
using LagoVista.UserAdmin.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Resources;

[assembly: NeutralResourcesLanguage("en")]
namespace LagoVista.IoT.Web.Common
{
    public class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {

            services.AddTransient<IMetricsManager, Managers.MetricsManager>();
            services.AddTransient<IMetricsRepo, Repos.MetricsRepos>();
            services.AddTransient<ICacheAborter, CacheAborter>();
            services.AddTransient<IEntryIntentService, EntryIntentService>();
            services.AddTransient<IMetricsBySessionRepo, Repos.MetricsBySessionRepo>()

            services.AddTransient<IMetricsLoggerSettings, MetricsLoggerSettings>();
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
