using System.Threading;
using System.Threading.Tasks;
using LagoVista.Core;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.Notifications.Contracts;
using LagoVista.Notifications.Execution;
using LagoVista.Notifications.Models;
using LagoVista.IoT.Web.Common.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [Authorize]
    [Route("api/notifications/browser")]
    public sealed class BrowserNotificationEndpointsController : ControllerBase
    {
        private BrowserNotificationRegistration Registration =>
            HttpContext.RequestServices.GetService<BrowserNotificationRegistration>();

        private const string Disabled = "browser-notifications-disabled";
        private void NoStore() => Response.Headers.CacheControl = "no-store";

        [HttpGet("config")]
        public async Task<InvokeResult<WebPushPublicConfiguration>> Configuration(CancellationToken ct)
        {
            NoStore();
            var registration = Registration;
            return registration == null ? InvokeResult<WebPushPublicConfiguration>.FromError("Browser notifications are disabled.", Disabled)
                : await registration.ConfigurationAsync(HttpContext, ct);
        }

        [HttpGet("endpoints")]
        public async Task<ListResponse<EndpointStatus>> List(CancellationToken ct)
        {
            NoStore();
            var registration = Registration;
            return registration == null ? ListResponse<EndpointStatus>.FromError("Browser notifications are disabled.", Disabled)
                : await registration.ListAsync(HttpContext, ct);
        }

        [HttpPost("endpoints")]
        [Consumes("application/json")]
        [RequestSizeLimit(64 * 1024)]
        public async Task<InvokeResult<EndpointStatus>> Register([FromBody] EndpointRegistrationRequest request, CancellationToken ct)
        {
            NoStore();
            var registration = Registration;
            if (registration == null) return InvokeResult<EndpointStatus>.FromError("Browser notifications are disabled.", Disabled);
            if (!ModelState.IsValid || request == null)
                return InvokeResult<EndpointStatus>.FromError("Invalid notification registration material.", "registration-material-invalid");
            return await registration.RegisterAsync(HttpContext, request, ct);
        }

        [HttpPost("endpoints/{id}/unregister")]
        [Consumes("application/json")]
        public async Task<InvokeResult> Unregister(string id, [FromBody] UnregisterNotificationEndpointRequest request, CancellationToken ct)
        {
            NoStore();
            var registration = Registration;
            if (registration == null) return InvokeResult.FromError("Browser notifications are disabled.", Disabled);
            if (!ModelState.IsValid || id?.Length != 32 || !NormalizedId32.TryCreate(id, out var endpointId) || request == null)
                return InvokeResult.FromError("Generation and ETag required.", "generation-conflict");
            return await registration.UnregisterAsync(HttpContext, endpointId, request.Generation, request.ETag, ct);
        }
    }
}
