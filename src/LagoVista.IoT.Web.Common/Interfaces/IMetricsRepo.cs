using LagoVista.Core.Models.UIMetaData;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common
{
    public interface IMetricsRepo
    {
        Task WriteAsync(Models.WebSiteMetric metric);
        Task<ListResponse<Models.WebSiteMetric>> GetMetricsAsync(ListRequest request, string sessionId = null, string campaignId = null, string eventId = null);
    }
}
