// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 8a5091fb077f6bb8e821f94b19befce56b2f2cf51f5bb17c44bc83f98a2d5d7a
// IndexVersion: 0
// --- END CODE INDEX META ---
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
