using LagoVista.Core.Models.Diagnostics;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [SystemAdmin]
    [ApiController]
    public class PlatformSmokeTestsController : ControllerBase
    {
        private readonly IPlatformSmokeTestManager _manager;

        public PlatformSmokeTestsController(IPlatformSmokeTestManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        [HttpPost("/api/diagnostics/smoke-tests/run")]
        public Task<PlatformSmokeTestDashboard> RunAsync(CancellationToken cancellationToken)
        {
            return _manager.RunAsync(cancellationToken);
        }
    }
}
