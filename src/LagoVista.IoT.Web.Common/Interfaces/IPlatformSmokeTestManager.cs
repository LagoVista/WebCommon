using LagoVista.Core.Models.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Interfaces
{
    public interface IPlatformSmokeTestManager
    {
        Task<PlatformSmokeTestDashboard> RunAsync(CancellationToken cancellationToken = default);
    }
}
