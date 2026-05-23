
using LagoVista.Core.Rcg.Client.Interfaces;
using LagoVista.Core.Rcg.Client.Models;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [ApiController]
    [Route("api/rcg/status")]
    public class RcgStatusController : LagoVistaBaseController
    {
        private readonly IRcgStatusManager _statusManager;

        public RcgStatusController(IRcgStatusManager statusManager, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
        }

        [HttpGet("diagnostics")]
        public Task<InvokeResult<RemoteControlDiagnosticsSnapshot>> GetDiagnosticsAsync()
        {
            return _statusManager.GetDiagnosticsAsync(OrgEntityHeader, UserEntityHeader);
        }
    }
}
