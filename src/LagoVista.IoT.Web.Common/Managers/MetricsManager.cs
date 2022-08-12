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
        private readonly IMetricsBySessionRepo _sessionRepo;

        public MetricsManager(IMetricsRepo repo, IMetricsBySessionRepo sessionRepo)
        {
            this._repo = repo ?? throw new ArgumentNullException(nameof(repo));
            this._sessionRepo = sessionRepo ?? throw new ArgumentNullException(nameof(sessionRepo));
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

 

        public async Task WriteAsync(MetricsInfo info, string ipAddress)
        {
            RequestCountByMethod.WithLabels(info.FullPath, info.SessionId, info.CampaignId, info.EventId, info.EventData).Inc();

            if (info.FullPath.StartsWith("/site/job"))
            {
                HiringEventsMethod.WithLabels(info.FullPath, info.SessionId, info.CampaignId, info.EventId, info.EventData).Inc();
            }

            ipAddress = String.IsNullOrEmpty(ipAddress) ? "?" : ipAddress;
            var metric = Models.WebSiteMetricByPath.FromMetricsInfo(info, ipAddress);

            await _repo.WriteAsync(metric);
            await _sessionRepo.WriteAsync(Models.WebSiteMetricBySession.FromMetricsInfo(info, ipAddress));
        }

        public Task<ListResponse<WebSiteMetric>> GetAllMetricsAsync(ListRequest request)
        {
            return _repo.GetAllMetricsAsync(request);
        }

        public Task<ListResponse<WebSiteMetric>> GetMetricsBySessionAsync(ListRequest request, string session)
        {
            return _sessionRepo.GetMetricsAsync(request, session);
        }

        public Task<ListResponse<WebSiteMetric>> GetMetricsByPathAsync(ListRequest request, string path)
        {
            return _repo.GetMetricsAsync(request, path);
        }
    }
}
