// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 09277037a2c30e1450d9dcd7cddfd7a14ac12d55a0b2a44d42f5e95b56f1e6de
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Models;
using System.Threading.Tasks;
using System.Linq;

namespace LagoVista.IoT.Web.Common.Repos
{
    public class MetricsRepos :  TableStorageBase<WebSiteMetricByPath>, IMetricsRepo
    {
        public MetricsRepos(IMetricsLoggerSettings settings, IAdminLogger logger) : 
            base(settings.MetricsLoggerStorage.AccountId, settings.MetricsLoggerStorage.AccessKey, logger)
        {
        }

        public async Task<ListResponse<WebSiteMetric>> GetAllMetricsAsync(ListRequest request)
        {
            var metrics = await GetPagedResultsAsync(request);
            return ListResponse<WebSiteMetric>.Create(metrics.Model.Select(rqst => WebSiteMetric.FromByPath(rqst)));
        }

        public async Task<ListResponse<WebSiteMetric>> GetMetricsAsync(ListRequest request, string path)
        {
            var metrics = await GetPagedResultsAsync(path, request);
            return ListResponse<WebSiteMetric>.Create(metrics.Model.Select(rqst => WebSiteMetric.FromByPath(rqst)));
        }

        public Task WriteAsync(WebSiteMetricByPath metric)
        {
            return InsertAsync(metric);
        }    
    }
}
