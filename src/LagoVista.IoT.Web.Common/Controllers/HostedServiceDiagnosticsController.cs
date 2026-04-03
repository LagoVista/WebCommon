using LagoVista.Core.Models.Diagnostics;
using LagoVista.IoT.Web.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
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
}