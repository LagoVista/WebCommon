using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/process/mcp")]
    public sealed class McpCapabilityController : ControllerBase
    {
        private readonly IMcpCapabilityService _service;

        public McpCapabilityController(IMcpCapabilityService service)
        {
            _service = service;
        }

        [HttpPost("servers")]
        public async Task<McpServerCapabilitySnapshot> Register([FromBody] McpServerConnection connection)
        {
            await _service.RegisterAsync(connection);
            return await _service.TestConnectionAsync(connection.Id);
        }

        [HttpGet("servers")]
        public Task<IReadOnlyCollection<McpServerCapabilitySnapshot>> Servers()
            => _service.GetRegisteredServersAsync();

        [HttpPost("servers/{serverId}/test")]
        public Task<McpServerCapabilitySnapshot> Test(string serverId)
            => _service.TestConnectionAsync(serverId);

        [HttpPost("servers/{serverId}/discover")]
        public Task<McpServerCapabilitySnapshot> Discover(string serverId)
            => _service.DiscoverAsync(serverId);

        [HttpPost("invoke")]
        public Task<McpToolInvocationResult> Invoke([FromBody] McpToolInvocationRequest request)
            => _service.InvokeAsync(request);
    }
}
