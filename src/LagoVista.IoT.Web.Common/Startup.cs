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
        }
    }
}
