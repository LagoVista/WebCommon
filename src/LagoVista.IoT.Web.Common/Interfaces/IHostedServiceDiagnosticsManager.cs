using LagoVista.Core.Models.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Interfaces
{
    public interface IHostedServiceDiagnosticsManager
    {
        Task<HostedServiceDiagnosticDashboard> GetLocalHostedServiceDiagnosticsAsync();
        Task<List<HostedServiceDiagnosticPodTarget>> GetDiscoveredPodsAsync();
        Task<HostedServiceDiagnosticClusterDashboard> GetClusterHostedServiceDiagnosticsAsync();
    }
}
