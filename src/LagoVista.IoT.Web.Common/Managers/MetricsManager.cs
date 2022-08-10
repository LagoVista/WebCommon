using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Web.Common.Models;
using Prometheus;
using System;

using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Managers
{
    public class MetricsManager : IMetricsManager
    {
        private readonly IMetricsRepo _repo;

        public MetricsManager(IMetricsRepo repo)
        {
            this._repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }


        private static readonly Counter RequestCountByMethod = Metrics.CreateCounter("nuviot_page_views", "Number of requests received, by HTTP method.",
                       new CounterConfiguration
                       {
                   // Here you specify only the names of the labels.
                   LabelNames = new[] { "path", "sessionid", "campaignid", "eventid", "eventdata" }
                       });

        private static readonly Counter HiringEventsMethod = Metrics.CreateCounter("hiring_page_views", "Number of requests received, by HTTP method.",
               new CounterConfiguration
               {
                   // Here you specify only the names of the labels.
                   LabelNames = new[] { "path", "sessionid", "campaignid", "eventid", "eventdata" }
               });

        public Task<ListResponse<WebSiteMetric>> GetMetricsAsync(ListRequest request, string sessionId = null, string campaignId = null, string eventId = null)
        {
            return this._repo.GetMetricsAsync(request, sessionId, campaignId, eventId);
        }

        public async Task WriteAsync(MetricsInfo info, string ipAddress)
        {
            RequestCountByMethod.WithLabels(info.FullPath, info.SessionId, info.CampaignId).Inc();

            if (info.FullPath.StartsWith("/site/job"))
            {
                HiringEventsMethod.WithLabels(info.FullPath, info.SessionId, info.CampaignId).Inc();
            }

            ipAddress = String.IsNullOrEmpty(ipAddress) ? "?" : ipAddress;
            var metric = Models.WebSiteMetric.FromMetricsInfo(info, ipAddress);

            await _repo.WriteAsync(metric);
        }
    }
}
