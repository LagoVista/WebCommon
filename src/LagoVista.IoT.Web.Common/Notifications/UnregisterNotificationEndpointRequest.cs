namespace LagoVista.IoT.Web.Common.Notifications
{
    public sealed class UnregisterNotificationEndpointRequest
    {
        public long Generation { get; set; }
        public string ETag { get; set; }
    }
}
