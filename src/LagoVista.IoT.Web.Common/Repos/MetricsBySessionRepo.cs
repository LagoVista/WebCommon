using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Models;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Repos
{
    public class MetricsBySessionRepo :  TableStorageBase<WebSiteMetricBySession>, IMetricsBySessionRepo
    {
        public MetricsBySessionRepo(IMetricsLoggerSettings settings, IAdminLogger logger) : 
            base(settings.MetricsLoggerStorage.AccountId, settings.MetricsLoggerStorage.AccessKey, logger)
        {
        }

        public async Task<ListResponse<WebSiteMetric>> GetMetricsAsync(ListRequest request, string sessionId)
        {
            var metrics = await GetPagedResultsAsync(sessionId, request);
            return ListResponse<WebSiteMetric>.Create(metrics.Model.Select(rqst => WebSiteMetric.FromBySession(rqst)));
        }

        public Task WriteAsync(WebSiteMetricBySession metric)
        {
            return InsertAsync(metric);
        }
    }
}
