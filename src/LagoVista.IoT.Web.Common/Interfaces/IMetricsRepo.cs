// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: ca34e964555a0d96963411297105b03263009b1277431e1050308e131eb713ae
// IndexVersion: 2
// --- END CODE INDEX META ---
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
