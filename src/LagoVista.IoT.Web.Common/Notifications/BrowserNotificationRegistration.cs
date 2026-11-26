using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LagoVista.AspNetCore.Identity.Interfaces;
using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.AspNetCore.Identity.Services;
using LagoVista.Core;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.Notifications.Contracts;
using LagoVista.Notifications.Execution;
using LagoVista.Notifications.Models;
using LagoVista.UserAdmin.Interfaces.Repos.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace LagoVista.IoT.Web.Common.Notifications
{
    /// <summary>
    /// Browser-cookie projection into the existing canonical notification engine.
    /// The cookie scheme, persistent continuity cookie, user and AppInstance
    /// must agree; a browser ID or request DTO never grants authority.
    /// </summary>
    public sealed class BrowserNotificationRegistration
    {
        private readonly IBrowserInstallationApplicationResolver _applications;
        private readonly BrowserInstallationSession _session;
        private readonly IAuthenticationSchemeProvider _schemes;
        private readonly IAppUserRepo _users;
        private readonly INotificationEndpointService _endpoints;
        private readonly WebPushConfigurationDiscovery _discovery;
        private readonly string _environment;

        public BrowserNotificationRegistration(IBrowserInstallationApplicationResolver applications,
            BrowserInstallationSession session, IAuthenticationSchemeProvider schemes, IAppUserRepo users,
            INotificationEndpointService endpoints, WebPushConfigurationDiscovery discovery, string environment)
        {
            _applications = applications;
            _session = session;
            _schemes = schemes;
            _users = users;
            _endpoints = endpoints;
            _discovery = discovery;
            _environment = environment;
        }

        private const string Reauth = "reauthentication-required";
        private const string Denied = "binding-denied";
        private const string Unavailable = "configuration-or-credential-unavailable";
        private static InvokeResult<T> Error<T>(string code) => InvokeResult<T>.FromError("Browser notification operation unavailable.", code);

        private async Task<(NotificationCaller Caller, ClaimsPrincipal Principal, string Origin, string Error)> ResolveAsync(
            HttpContext context, bool mutation, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (_applications == null || _session == null || string.IsNullOrWhiteSpace(_environment) ||
                    await _schemes.GetSchemeAsync(IdentityConstants.ApplicationScheme) == null)
                    return (null, null, null, Unavailable);

                var authenticated = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
                if (!authenticated.Succeeded || authenticated.Principal == null)
                    return (null, null, null, Reauth);
                var principal = authenticated.Principal;
                var users = principal.FindAll(ClaimsFactory.CurrentUserId).ToArray();
                if (users.Length != 1 || string.IsNullOrWhiteSpace(users[0].Value) ||
                    principal.FindAll(ClaimsFactory.AppInstanceId).Count() != 1)
                    return (null, null, null, Reauth);
                var app = await _applications.ResolveAsync(context);
                if (app.Origin == null || string.IsNullOrWhiteSpace(app.AppId))
                    return (null, null, null, Unavailable);
                var origin = app.Origin.GetLeftPart(UriPartial.Authority);
                // Browser mutations use standard JSON requests and the browser's
                // existing Origin header, not a custom auth header or new CSRF token.
                if (mutation && (context.Request.Headers.Origin.Count != 1 ||
                    !string.Equals(context.Request.Headers.Origin[0], origin, StringComparison.Ordinal)))
                    return (null, null, null, Denied);
                var user = await _users.FindByIdAsync(users[0].Value);
                var instance = await _session.ReadBoundSessionAsync(context, principal, user, app.AppId, app.Origin);
                ct.ThrowIfCancellationRequested();
                return (new NotificationCaller(user.Id.Value, principal.FindFirst(ClaimsFactory.CurrentOrgId)?.Value,
                    user.Id.Value, instance.AppId, instance.RowKey), principal, origin, null);
            }
            catch (OperationCanceledException) { throw; }
            catch (InvalidOperationException) { return (null, null, null, Reauth); }
            catch (Exception) { return (null, null, null, Unavailable); }
        }

        public async Task<InvokeResult<WebPushPublicConfiguration>> ConfigurationAsync(HttpContext context, CancellationToken ct)
        {
            var binding = await ResolveAsync(context, false, ct);
            return binding.Caller == null ? Error<WebPushPublicConfiguration>(binding.Error) :
                await _discovery.GetAsync(binding.Caller, _environment, binding.Origin, ct);
        }

        public async Task<InvokeResult<EndpointStatus>> RegisterAsync(HttpContext context, EndpointRegistrationRequest request, CancellationToken ct)
        {
            var binding = await ResolveAsync(context, true, ct);
            if (binding.Caller == null) return Error<EndpointStatus>(binding.Error);
            if (request == null || request.ProviderKey != "webpush" || request.Environment != _environment || !request.Consented)
                return Error<EndpointStatus>("registration-material-invalid");
            var config = await _discovery.GetAsync(binding.Caller, _environment, binding.Origin, ct);
            if (!config.Successful) return Error<EndpointStatus>(config.Errors.FirstOrDefault()?.ErrorCode ?? Unavailable);
            if (!request.ProviderConfigurationId.Equals(config.Result.ConfigurationId))
                return Error<EndpointStatus>(Denied);
            var adapter = new EndpointRegistrationAdapter(_endpoints,
                (principal, _) => Task.FromResult(ReferenceEquals(principal, binding.Principal) ? binding.Caller : null));
            return await adapter.RegisterAsync(binding.Principal, request, ct);
        }

        public async Task<ListResponse<EndpointStatus>> ListAsync(HttpContext context, CancellationToken ct)
        {
            var binding = await ResolveAsync(context, false, ct);
            if (binding.Caller == null) return ListResponse<EndpointStatus>.FromError("Browser notification operation unavailable.", binding.Error);
            var result = await _endpoints.ListAsync(binding.Caller, binding.Caller.AppId, ct);
            return result.Successful ? ListResponse<EndpointStatus>.Create(
                result.Result.Where(e => e.ProviderKey == "webpush" && e.Environment == _environment)) :
                ListResponse<EndpointStatus>.FromError("Browser notification operation unavailable.", result.Errors.FirstOrDefault()?.ErrorCode ?? Denied);
        }

        public async Task<InvokeResult> UnregisterAsync(HttpContext context, NormalizedId32 id, long generation, string etag, CancellationToken ct)
        {
            var binding = await ResolveAsync(context, true, ct);
            if (binding.Caller == null) return InvokeResult.FromError("Browser notification operation unavailable.", binding.Error);
            if (generation <= 0 || string.IsNullOrWhiteSpace(etag))
                return InvokeResult.FromError("Generation and ETag required.", "generation-conflict");
            var result = await _endpoints.ListAsync(binding.Caller, binding.Caller.AppId, ct);
            if (!result.Successful || !result.Result.Any(e => e.Id.Equals(id) && e.ProviderKey == "webpush" && e.Environment == _environment))
                return InvokeResult.FromError("Browser notification operation unavailable.", Denied);
            return await _endpoints.UnregisterAsync(binding.Caller, id, generation, etag, ct);
        }
    }
}
