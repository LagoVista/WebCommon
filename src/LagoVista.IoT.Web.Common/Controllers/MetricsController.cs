using LagoVista.Core.Models.UIMetaData;
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
        public async Task PostMetric([FromBody] MetricsInfo info)
        {
            var ipAddress = String.IsNullOrEmpty(HttpContext.Connection?.RemoteIpAddress?.ToString()) ? "?" : HttpContext.Connection?.RemoteIpAddress?.ToString();
            await this._manager.WriteAsync(info, ipAddress);
        }

        [Authorize]
        [HttpGet("/web/sitemetrics")]
        public async Task<ListResponse<WebSiteMetric>> GetMetricsAsync(string sessionid = null, string campaignid = null, string eventid = null)
        {
            return await this._manager.GetMetricsAsync(GetListRequestFromHeader(), sessionid, campaignid, eventid);
        }
    }
}