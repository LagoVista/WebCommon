using LagoVista.Core.Models.Diagnostics;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [SystemAdmin]
    [ApiController]
    public class HostedServiceDiagnosticsController : ControllerBase
    {
        private readonly IHostedServiceDiagnosticsManager _manager;

        public HostedServiceDiagnosticsController(IHostedServiceDiagnosticsManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        [HttpGet("/api/diagnostics/hostedservices/local")]
        public Task<HostedServiceDiagnosticDashboard> GetLocalHostedServiceDiagnosticsAsync()
        {
            return _manager.GetLocalHostedServiceDiagnosticsAsync();
        }

        [HttpGet("/api/diagnostics/hostedservices/pods")]
        public Task<List<HostedServiceDiagnosticPodTarget>> GetDiscoveredPodsAsync()
        {
            return _manager.GetDiscoveredPodsAsync();
        }

        [HttpGet("/api/diagnostics/hostedservices/cluster")]
        public Task<HostedServiceDiagnosticClusterDashboard> GetClusterHostedServiceDiagnosticsAsync()
        {
            return _manager.GetClusterHostedServiceDiagnosticsAsync();
        }
    }

    [ApiController]
    public class PublicHostedServiceDiagnosticsController : ControllerBase
    {
        private readonly IHostedServiceDiagnosticsManager _manager;

        public PublicHostedServiceDiagnosticsController(IHostedServiceDiagnosticsManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        [HttpGet("/api/diagnostics/hostedservices/health")]
        public async Task<IActionResult> GetPublicHostedServiceHealthAsync()
        {
            var dashboard = await _manager.GetClusterHostedServiceDiagnosticsAsync();

            var instanceCount = dashboard.InstanceCount;
            int reachableInstanceCount = dashboard.Instances.Count(instance => instance.Successful && instance.Dashboard != null);
            var unreachableInstanceCount = dashboard.Instances.Count(instance => !instance.Successful || instance.Dashboard == null);

            var services = dashboard.Instances
                .Where(instance => instance.Successful && instance.Dashboard != null)
                .SelectMany(instance => instance.Dashboard.Services)
                .ToList();

            var runningServiceCount = services.Count(service => service.Status == HostedServiceDiagnosticStatus.Running);
            var startingServiceCount = services.Count(service => service.Status == HostedServiceDiagnosticStatus.Starting);
            var stoppedServiceCount = services.Count(service => service.Status == HostedServiceDiagnosticStatus.Stopped);
            var errorServiceCount = services.Count(service => service.Status == HostedServiceDiagnosticStatus.Error);
            var staleServiceCount = services.Count(service => IsServiceStale(service, dashboard.GeneratedUtc));

            var unhealthyServiceCount = errorServiceCount + stoppedServiceCount + staleServiceCount;
            var degradedServiceCount = startingServiceCount;

            var status = "Healthy";
            var reasonCode = "AllServicesHealthy";
            var httpStatusCode = StatusCodes.Status200OK;

            if (unreachableInstanceCount > 0 || unhealthyServiceCount > 0)
            {
                status = "Unhealthy";
                reasonCode = unreachableInstanceCount > 0 ? "OneOrMoreInstancesUnavailable" : "OneOrMoreServicesUnhealthy";
                httpStatusCode = StatusCodes.Status503ServiceUnavailable;
            }
            else if (degradedServiceCount > 0)
            {
                status = "Degraded";
                reasonCode = "OneOrMoreServicesStarting";
                httpStatusCode = StatusCodes.Status200OK;
            }

            var result = new
            {
                Status = status,
                ReasonCode = reasonCode,
                GeneratedUtc = dashboard.GeneratedUtc,
                EnvironmentName = dashboard.EnvironmentName,
                InstanceCount = instanceCount,
                ReachableInstanceCount = reachableInstanceCount,
                UnreachableInstanceCount = unreachableInstanceCount,
                ServiceCount = services.Count,
                RunningServiceCount = runningServiceCount,
                StartingServiceCount = startingServiceCount,
                ErrorServiceCount = errorServiceCount,
                StoppedServiceCount = stoppedServiceCount,
                StaleServiceCount = staleServiceCount,
                HealthyServiceCount = runningServiceCount - staleServiceCount,
                UnhealthyServiceCount = unhealthyServiceCount
            };

            return StatusCode(httpStatusCode, result);
        }

        private static bool IsServiceStale(HostedServiceDiagnosticSnapshot service, DateTime generatedUtc)
        {
            if (service.ExpectedActivityWindowSeconds <= 0)
            {
                return false;
            }

            if (!service.LastActivityUtc.HasValue)
            {
                return true;
            }

            var lastActivityAge = generatedUtc.ToUniversalTime() - service.LastActivityUtc.Value.ToUniversalTime();

            return lastActivityAge.TotalSeconds > service.ExpectedActivityWindowSeconds;
        }
    }
}