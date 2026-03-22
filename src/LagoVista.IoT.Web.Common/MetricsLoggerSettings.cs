using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LagoVista.IoT.Web.Common
{
    public class MetricsLoggerSettings : IMetricsLoggerSettings
    {
        public IConnectionSettings MetricsLoggerStorage { get; }

        public MetricsLoggerSettings(IConfiguration configuration) 
        {
            MetricsLoggerStorage = configuration.CreateDefaultTableStorageSettings();
        }
    }
}
