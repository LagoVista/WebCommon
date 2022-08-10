using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Models;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Repos
{
    public class MetricsRepos :  TableStorageBase<WebSiteMetric>, IMetricsRepo
    {
        public MetricsRepos(IMetricsLoggerSettings settings, IAdminLogger logger) : 
            base(settings.MetricsLoggerStorage.AccountId, settings.MetricsLoggerStorage.AccessKey, logger)
        {
        }

        public Task<ListResponse<WebSiteMetric>> GetMetricsAsync(ListRequest request, string sessionId = null, string campaignId = null, string eventId = null)
        {
            throw new System.NotImplementedException();
        }

        public Task WriteAsync(WebSiteMetric metric)
        {
            return InsertAsync(metric);
        }
    }
}
