namespace LagoVista.IoT.Web.Common.Models
{

    public class MetricsInfo
    {
        private string _campaignId = "?";
        private string _eventId = "?";
        private string _eventData = "?";

        public string FullPath { get; set; }
        public string SessionId { get; set; }

        public string CampaignId
        {
            get => _campaignId;
            set => _campaignId = string.IsNullOrEmpty(value) ? value : "?";
        }

        public string EventId
        {
            get => _eventId;
            set => _eventId = string.IsNullOrEmpty(value) ? value : "?";
        }

        public string EventData
        {
            get => _eventData;
            set => _eventData = string.IsNullOrEmpty(value) ? value : "?";
        }
    }
}
