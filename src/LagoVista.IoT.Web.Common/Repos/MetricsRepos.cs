using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Models;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Repos
{
    public class MetricsRepos :  TableStorageBase<WebSiteMetricByPath>, IMetricsRepo
    {
        public MetricsRepos(IMetricsLoggerSettings settings, IAdminLogger logger) : 
            base(settings.MetricsLoggerStorage.AccountId, settings.MetricsLoggerStorage.AccessKey, logger)
        {
        }

        public Task<ListResponse<WebSiteMetric>> GetAllMetricsAsync(ListRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<ListResponse<WebSiteMetric>> GetMetricsAsync(ListRequest request, string path)
        {
            throw new System.NotImplementedException();
        }

        public Task WriteAsync(WebSiteMetricByPath metric)
        {
            return InsertAsync(metric);
        }    
    }
}
