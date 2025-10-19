// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 12c3f3d33ccc16cfd1348c44af877a8f4b4a47ad19fdcba9d401fbe96b0b54d3
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Models;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    public class MetricsController : LagoVistaBaseController
    {
        private readonly IMetricsManager _manager;

        public MetricsController(UserManager<AppUser> userManager, IAdminLogger logger, IMetricsManager manager) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }        

        [HttpPost("/web/logmetric")]
        public InvokeResult PostMetric([FromBody] MetricsInfo info)
        {
            var ipAddress = String.IsNullOrEmpty(HttpContext.Connection?.RemoteIpAddress?.ToString()) ? "?" : HttpContext.Connection?.RemoteIpAddress?.ToString();
            this._manager.WriteAsync(info, ipAddress);
            return InvokeResult.Success;
        }

        [Authorize]
        [HttpGet("/web/sitemetrics")]
        public async Task<ListResponse<WebSiteMetric>> GetMetricsAsync()
        {
            return await this._manager.GetAllMetricsAsync(GetListRequestFromHeader());
        }

        [Authorize]
        [HttpGet("/web/sitemetrics/session/{sessionid}")]
        public async Task<ListResponse<WebSiteMetric>> GetMetricsBySessionAsync(string sessionid)
        {
            return await this._manager.GetMetricsBySessionAsync(GetListRequestFromHeader(), sessionid);
        }

        [Authorize]
        [HttpGet("/web/sitemetrics/path/{path}")]
        public async Task<ListResponse<WebSiteMetric>> GetMetricsByPathAsync(string path)
        {
            return await this._manager.GetMetricsBySessionAsync(GetListRequestFromHeader(), path);
        }
    }
}