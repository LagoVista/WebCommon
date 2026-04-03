using LagoVista.Core.Models.Diagnostics;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Interfaces.Services
{
    public interface IHostedServiceClusterDiagnosticsService
    {
        Task<HostedServiceDiagnosticClusterDashboard> GetClusterHostedServiceDiagnosticsAsync();
    }
}