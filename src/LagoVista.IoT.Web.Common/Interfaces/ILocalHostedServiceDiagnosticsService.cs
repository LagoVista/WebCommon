using LagoVista.Core.Models.Diagnostics;
using System.Threading.Tasks;

public interface ILocalHostedServiceDiagnosticsService
{
    Task<HostedServiceDiagnosticDashboard> GetLocalHostedServiceDiagnosticsAsync();
}