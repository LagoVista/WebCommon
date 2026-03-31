using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.Dignostics;
using LagoVista.IoT.Web.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Managers
{
    public class HostedServiceDiagnosticsManager : IHostedServiceDiagnosticsManager
    {
        private readonly IEnumerable<IHostedServiceDiagnostics> _hostedServiceDiagnostics;

        public HostedServiceDiagnosticsManager(IEnumerable<IHostedServiceDiagnostics> hostedServiceDiagnostics)
        {
            _hostedServiceDiagnostics = hostedServiceDiagnostics ?? throw new ArgumentNullException(nameof(hostedServiceDiagnostics));
        }

        public Task<HostedServiceDiagnosticDashboard> GetLocalHostedServiceDiagnosticsAsync()
        {
            var dashboard = new HostedServiceDiagnosticDashboard
            {
                InstanceName = Environment.MachineName,
                EnvironmentName = GetEnvironmentName(),
                Version = GetVersion(),
                GeneratedUtc = DateTime.UtcNow,
                Services = _hostedServiceDiagnostics
                    .Select(diag => SafeGetSnapshot(diag))
                    .OrderBy(snapshot => snapshot.Name)
                    .ToList()
            };

            return Task.FromResult(dashboard);
        }

        private HostedServiceDiagnosticSnapshot SafeGetSnapshot(IHostedServiceDiagnostics diagnostics)
        {
            try
            {
                var snapshot = diagnostics.GetSnapshot() ?? new HostedServiceDiagnosticSnapshot();

                if (String.IsNullOrWhiteSpace(snapshot.Name))
                {
                    snapshot.Name = diagnostics.Name;
                }

                snapshot.RecentEntries ??= new List<HostedServiceDiagnosticLogEntry>();

                return snapshot;
            }
            catch (Exception ex)
            {
                return new HostedServiceDiagnosticSnapshot
                {
                    Name = diagnostics.Name,
                    Status = HostedServiceDiagnosticStatus.Error,
                    LastErrorUtc = DateTime.UtcNow,
                    LastError = $"Failed to get hosted service snapshot. {ex.Message}",
                    RecentEntries = new List<HostedServiceDiagnosticLogEntry>
                    {
                        new HostedServiceDiagnosticLogEntry
                        {
                            TimestampUtc = DateTime.UtcNow,
                            Message = $"Snapshot failure: {ex.Message}"
                        }
                    }
                };
            }
        }

        private static string GetEnvironmentName()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Unknown";
        }

        private static string GetVersion()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            return assembly.GetName().Version?.ToString() ?? "Unknown";
        }
    }
}