using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Models;

namespace LagoVista.IoT.Web.Common.Interfaces
{
    public class MetricsRepos :  TableStorageBase<WebSiteMetric>
    {
        public MetricsRepos(IMetricsLoggerSettings settings, IAdminLogger logger) : 
            base(settings.MetricsLoggerStorage.AccountId, settings.MetricsLoggerStorage.AccessKey, logger)
        {
        }
    }
}
