using LagoVista.Core.Models.Diagnostics;
using LagoVista.IoT.Web.Common.Interfaces;
using LagoVista.IoT.Web.Common.Interfaces.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Services
{
    public class HostedServiceClusterDiagnosticsService : IHostedServiceClusterDiagnosticsService
    {
        private readonly IKubernetesPodDiscoveryService _podDiscoveryService;
        private readonly ILocalHostedServiceDiagnosticsService _hostedServiceDiagnosticsManager;
        private readonly IHttpClientFactory _httpClientFactory;

        public HostedServiceClusterDiagnosticsService(IKubernetesPodDiscoveryService podDiscoveryService, ILocalHostedServiceDiagnosticsService hostedServiceDiagnosticsManager, IHttpClientFactory httpClientFactory)
        {
            _podDiscoveryService = podDiscoveryService ?? throw new ArgumentNullException(nameof(podDiscoveryService));
            _hostedServiceDiagnosticsManager = hostedServiceDiagnosticsManager ?? throw new ArgumentNullException(nameof(hostedServiceDiagnosticsManager));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<HostedServiceDiagnosticClusterDashboard> GetClusterHostedServiceDiagnosticsAsync()
        {
            var podTargets = await _podDiscoveryService.GetHostedServiceDiagnosticPodsAsync().ConfigureAwait(false);
            var instanceResults = await Task.WhenAll(podTargets.Select(GetInstanceResultAsync)).ConfigureAwait(false);

            return new HostedServiceDiagnosticClusterDashboard
            {
                GeneratedUtc = DateTime.UtcNow,
                EnvironmentName = GetEnvironmentName(),
                InstanceCount = instanceResults.Length,
                Instances = instanceResults
                    .OrderBy(instance => instance.Module)
                    .ThenBy(instance => instance.PodName)
                    .ToList()
            };
        }

        private async Task<HostedServiceDiagnosticInstanceResult> GetInstanceResultAsync(HostedServiceDiagnosticPodTarget podTarget)
        {
            try
            {
                HostedServiceDiagnosticDashboard dashboard;

                if (podTarget.IsCurrentPod)
                {
                    dashboard = await _hostedServiceDiagnosticsManager.GetLocalHostedServiceDiagnosticsAsync().ConfigureAwait(false);
                }
                else
                {
                    var client = _httpClientFactory.CreateClient("HostedServiceDiagnosticsCluster");
                    var endpoint = BuildLocalDiagnosticsEndpoint(podTarget);
                    dashboard = await client.GetFromJsonAsync<HostedServiceDiagnosticDashboard>(endpoint).ConfigureAwait(false);
                }

                return new HostedServiceDiagnosticInstanceResult
                {
                    PodName = podTarget.PodName,
                    PodIp = podTarget.PodIp,
                    Module = podTarget.Module,
                    Tier = podTarget.Tier,
                    NodeName = podTarget.NodeName,
                    IsCurrentPod = podTarget.IsCurrentPod,
                    Successful = dashboard != null,
                    Error = dashboard == null ? "No dashboard payload returned." : null,
                    Dashboard = dashboard
                };
            }
            catch (Exception ex)
            {
                return new HostedServiceDiagnosticInstanceResult
                {
                    PodName = podTarget.PodName,
                    PodIp = podTarget.PodIp,
                    Module = podTarget.Module,
                    Tier = podTarget.Tier,
                    NodeName = podTarget.NodeName,
                    IsCurrentPod = podTarget.IsCurrentPod,
                    Successful = false,
                    Error = ex.Message
                };
            }
        }

        private static string BuildLocalDiagnosticsEndpoint(HostedServiceDiagnosticPodTarget podTarget)
        {
            int port = 5000;
            switch (podTarget.Module)
            {
                case "api-host":
                    port = 5001;
                    break;
                default:
                    port = 5000;
                    break;
            }
            return $"http://{podTarget.PodIp}:{port}/api/diagnostics/hostedservices/local";
        }

        private static string GetEnvironmentName()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Unknown";
        }
    }
}