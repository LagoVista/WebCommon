using LagoVista.Core.Models.Diagnostics;
using LagoVista.IoT.Web.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Interfaces.Services
{
    public interface IKubernetesPodDiscoveryService
    {
        Task<List<HostedServiceDiagnosticPodTarget>> GetHostedServiceDiagnosticPodsAsync();
    }
}