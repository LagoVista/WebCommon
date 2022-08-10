using LagoVista.Core;
using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Models
{
    public class WebSiteMetric : TableStorageEntity
    {
        public string IPAddress { get; set; }
        public string FullPath { get; set; }
        public string SessionId { get; set; }
        public string CampaignId { get; set; }
        public string EventId { get; set; }
        public string EventData { get; set; }

        public static WebSiteMetric FromMetricsInfo(MetricsInfo info, string ipAddress)
        {
            if(!String.IsNullOrEmpty(info.FullPath))
            {
                info.FullPath = info.FullPath.Replace("/", ".").Substring(1);
            }
            else
            {
                info.FullPath = "root";
            }

            return new WebSiteMetric()
            {
                IPAddress = ipAddress,
                FullPath = info.FullPath,
                SessionId = info.SessionId,
                CampaignId = info.CampaignId,
                PartitionKey = info.FullPath,
                EventId = info.EventId,
                EventData = info.EventData,
                RowKey = DateTime.Now.ToInverseTicksRowKey()
            };
        }
    }
}
