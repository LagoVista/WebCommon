using LagoVista.Core.Models.UIMetaData;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common
{
    public interface IMetricsManager
    {
        Task WriteAsync(Models.MetricsInfo metric, string ipAddress);
        Task<ListResponse<Models.WebSiteMetric>> GetAllMetricsAsync(ListRequest request);
        Task<ListResponse<Models.WebSiteMetric>> GetMetricsBySessionAsync(ListRequest request, string sessionId);
        Task<ListResponse<Models.WebSiteMetric>> GetMetricsByPathAsync(ListRequest request, string pathId);
    }
}
