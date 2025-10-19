// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 3fa1889743ff1931f7616185aa7b85841d756f169ccb8a89043b5e9d5caca081
// IndexVersion: 0
// --- END CODE INDEX META ---
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
