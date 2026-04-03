using LagoVista.Core.Models.Diagnostics;
using LagoVista.IoT.Web.Common.Interfaces;
using LagoVista.IoT.Web.Common.Interfaces.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Managers
{
    public class HostedServiceDiagnosticsManager : IHostedServiceDiagnosticsManager
    {
        private readonly ILocalHostedServiceDiagnosticsService _localHostedServiceDiagnosticsService;
        private readonly IKubernetesPodDiscoveryService _kubernetesPodDiscoveryService;
        private readonly IHostedServiceClusterDiagnosticsService _hostedServiceClusterDiagnosticsService;

        public HostedServiceDiagnosticsManager(ILocalHostedServiceDiagnosticsService localHostedServiceDiagnosticsService, IKubernetesPodDiscoveryService kubernetesPodDiscoveryService, IHostedServiceClusterDiagnosticsService hostedServiceClusterDiagnosticsService)
        {
            _localHostedServiceDiagnosticsService = localHostedServiceDiagnosticsService;
            _kubernetesPodDiscoveryService = kubernetesPodDiscoveryService;
            _hostedServiceClusterDiagnosticsService = hostedServiceClusterDiagnosticsService;
        }

        public Task<HostedServiceDiagnosticDashboard> GetLocalHostedServiceDiagnosticsAsync()
        {
            return _localHostedServiceDiagnosticsService.GetLocalHostedServiceDiagnosticsAsync();
        }

        public Task<List<HostedServiceDiagnosticPodTarget>> GetDiscoveredPodsAsync()
        {
            return _kubernetesPodDiscoveryService.GetHostedServiceDiagnosticPodsAsync();
        }

        public Task<HostedServiceDiagnosticClusterDashboard> GetClusterHostedServiceDiagnosticsAsync()
        {
            return _hostedServiceClusterDiagnosticsService.GetClusterHostedServiceDiagnosticsAsync();
        }
    }
}