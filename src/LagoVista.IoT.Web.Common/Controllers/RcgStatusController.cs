
using LagoVista.Core.Rcg.Client.Interfaces;
using LagoVista.Core.Rcg.Client.Models;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [ApiController]
    [Route("api/rcg/status")]
    public class RcgStatusController : LagoVistaBaseController
    {
        private readonly IRcgStatusManager _statusManager;
        private readonly IRcgRpcClientTransport _rpcClientTransport;

        public RcgStatusController(IRcgStatusManager statusManager, IRcgRpcClientTransport rpcClientTransport, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
            _rpcClientTransport = rpcClientTransport ?? throw new ArgumentNullException(nameof(rpcClientTransport));
        }

        [HttpGet("diagnostics")]
        public Task<InvokeResult<RemoteControlDiagnosticsSnapshot>> GetDiagnosticsAsync()
        {
            return _statusManager.GetDiagnosticsAsync(OrgEntityHeader, UserEntityHeader);
        }

        [HttpGet("connections")]
        public async Task<InvokeResult<List<RemoteControlConnectionSummary>>> GetConnectionsAsync()
        {
            var result = await _statusManager.GetDiagnosticsAsync(OrgEntityHeader, UserEntityHeader);
            if (!result.Successful)
            {
                return InvokeResult<List<RemoteControlConnectionSummary>>.FromInvokeResult(result.ToInvokeResult());
            }

            return InvokeResult<List<RemoteControlConnectionSummary>>.Create(result.Result.Connections);
        }

        [HttpPost("connections/{targetInstanceId}/echo")]
        public Task<InvokeResult<RcgRpcClientTransportResponse>> EchoAsync(string targetInstanceId)
        {
            if (String.IsNullOrWhiteSpace(targetInstanceId)) throw new ArgumentNullException(nameof(targetInstanceId));

            var payload = Encoding.UTF8.GetBytes("Hello from the portal through RCG.");

            var request = new RcgRpcClientTransportRequest
            {
                TargetInstanceId = targetInstanceId,
                TimeoutSeconds = 30,
                Frame = new RemoteControlFrame
                {
                    FrameType = RemoteControlFrameTypes.Command,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    Method = "echo",
                    ContentType = "text/plain",
                    PayloadBase64 = Convert.ToBase64String(payload)
                }
            };

            return _rpcClientTransport.SendAsync(request, OrgEntityHeader, UserEntityHeader);
        }
    }
}
