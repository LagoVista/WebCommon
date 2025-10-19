// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: a1ca7c3cf2b98fe1644e090db237253dc15d9cb5c7ae439008fba301c11ff057
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.Core.Interfaces;
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
            services.AddTransient<IMetricsBySessionRepo, Repos.MetricsBySessionRepo>();
        }
    }
}
