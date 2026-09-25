using LagoVista.Core.PlatformSupport;

namespace LagoVista.IoT.Logging.Rest.Models
{
    /// <summary>
    /// Log entry submitted by a client application.
    /// </summary>
    public class ClientLogEntry
    {
        public LogLevel Level { get; set; } = LogLevel.Error;
        public string Tag { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Application { get; set; }
        public string Version { get; set; }
        public string Route { get; set; }
        public string RequestMethod { get; set; }
        public string RequestUri { get; set; }
        public int? StatusCode { get; set; }
    }
}
