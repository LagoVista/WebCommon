using LagoVista.Core.Models.UIMetaData;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common
{
    public interface IMetricsRepo
    {
        Task WriteAsync(Models.WebSiteMetricByPath metric);
        Task<ListResponse<Models.WebSiteMetric>> GetMetricsAsync(ListRequest request, string path);

        Task<ListResponse<Models.WebSiteMetric>> GetAllMetricsAsync(ListRequest request);
    }

    public interface IMetricsBySessionRepo
    {
        Task WriteAsync(Models.WebSiteMetricBySession metric);
        Task<ListResponse<Models.WebSiteMetric>> GetMetricsAsync(ListRequest request, string sessionId);
    }
}
