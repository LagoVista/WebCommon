using LagoVista.Core.Models.Dignostics;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Interfaces
{
    public interface IHostedServiceDiagnosticsManager
    {
        Task<HostedServiceDiagnosticDashboard> GetLocalHostedServiceDiagnosticsAsync();
    }
}
