// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: e2be02ace186635a8f616bf2f191cb18d5708cfca083c4b6e4593a66c27d45cb
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.Core;
using LagoVista.Core.Models;
using System;

namespace LagoVista.IoT.Web.Common.Models
{
    public class WebSiteMetricByPath : TableStorageEntity
    {
        public string IPAddress { get; set; }
        public string TimeStamp { get; set; }
        public string FullPath { get; set; }
        public string SessionId { get; set; }
        public string CampaignId { get; set; }
        public string EventId { get; set; }
        public string EventData { get; set; }

        public static WebSiteMetricByPath FromMetricsInfo(MetricsInfo info, string ipAddress)
        {
            var partitionKey = info.FullPath;

            if (!String.IsNullOrEmpty(partitionKey))
            {
                partitionKey = partitionKey.Replace("/", ".").Substring(1);
            }
            else
            {
                partitionKey = "root";
            }

            var parts = partitionKey.Split('.');
            if (parts.Length > 2)
                partitionKey = $"{parts[0]}.{parts[1]}";

            return new WebSiteMetricByPath()
            {
                TimeStamp = DateTime.UtcNow.ToJSONString(),
                IPAddress = ipAddress,
                FullPath = info.FullPath,
                SessionId = info.SessionId,
                CampaignId = info.CampaignId,
                PartitionKey = partitionKey,
                EventId = info.EventId,
                EventData = info.EventData,
                RowKey = DateTime.Now.ToInverseTicksRowKey()
            };
        }
    }

    public class WebSiteMetricBySession : TableStorageEntity
    {
        public string IPAddress { get; set; }
        public string TimeStamp { get; set; }
        public string FullPath { get; set; }
        public string SessionId { get; set; }
        public string CampaignId { get; set; }
        public string EventId { get; set; }
        public string EventData { get; set; }

        public static WebSiteMetricBySession FromMetricsInfo(MetricsInfo info, string ipAddress)
        {
            if (!String.IsNullOrEmpty(info.FullPath))
            {
                info.FullPath = info.FullPath.Replace("/", ".").Substring(1);
            }
            else
            {
                info.FullPath = "root";
            }

            return new WebSiteMetricBySession()
            {
                TimeStamp = DateTime.UtcNow.ToJSONString(),
                IPAddress = ipAddress,
                FullPath = info.FullPath,
                SessionId = info.SessionId,
                CampaignId = info.CampaignId,
                PartitionKey = info.SessionId,
                EventId = info.EventId,
                EventData = info.EventData,
                RowKey = DateTime.Now.ToInverseTicksRowKey()
            };
        }
    }

    public class WebSiteMetric
    {
        public static WebSiteMetric FromByPath(WebSiteMetricByPath metric)
        {
            return new WebSiteMetric()
            {
                CampaignId = metric.CampaignId,
                EventData = metric.EventData,
                EventId = metric.EventId,
                FullPath = metric.FullPath,
                IPAddress = metric.IPAddress,
                TimeStamp = metric.TimeStamp,
                SessionId = metric.SessionId,
            };
        }

        public static WebSiteMetric FromBySession(WebSiteMetricBySession metric)
        {
            return new WebSiteMetric()
            {
                CampaignId = metric.CampaignId,
                EventData = metric.EventData,
                EventId = metric.EventId,
                FullPath = metric.FullPath,
                IPAddress = metric.IPAddress,
                TimeStamp = metric.TimeStamp,
                SessionId = metric.SessionId
            };
        }

        public string SessionId { get; set; }
        public string TimeStamp { get; set; }
        public string IPAddress { get; set; }
        public string FullPath { get; set; }
        public string CampaignId { get; set; }
        public string EventId { get; set; }
        public string EventData { get; set; }
    }
}
