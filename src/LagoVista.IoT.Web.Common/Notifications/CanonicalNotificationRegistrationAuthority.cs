using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.StorageProviders;
using LagoVista.Core.Interfaces;
using LagoVista.DevOps.Models;
using LagoVista.Notifications.Contracts;
using LagoVista.Notifications.Execution;
using LagoVista.Notifications.Models;
using LagoVista.UserAdmin.Interfaces.Repos.Apps;
using LagoVista.UserAdmin.Interfaces.Repos.Orgs;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.IoT.Web.Common.Notifications
{
    // Host policy projection only. The common installation authority checks the canonical
    // user/partition/instance/app binding; the endpoint service owns all mutations and CAS.
    // Owner candidates are not grants: the service identity needs canonical membership.
    public sealed class CanonicalNotificationRegistrationAuthority : INotificationConfigurationAuthority
    {
        private readonly bool _enabled;
        private readonly string[] _owners;
        private readonly IDocumentStorageClientProvider _documents;
        private readonly IOrgUserRepo _members;
        private readonly ISystemUsers _system;
        private readonly IApplicationDataStore _store;
        private readonly bool _webPushEnabled;
        public CanonicalNotificationRegistrationAuthority(bool enabled, string[] owners,
            IDocumentStorageClientProvider documents, IOrgUserRepo members, ISystemUsers system,
            IApplicationDataStore store = null, bool webPushEnabled = false)
        {
            _enabled = enabled; _owners = owners; _documents = documents; _members = members; _system = system;
            _store = store; _webPushEnabled = webPushEnabled;
        }

        private async Task<bool> ActiveUserAsync(string user, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(user)) return false;
            var record = await _documents.GetClient().GetDocumentAsync<AppUser>(user, false);
            ct.ThrowIfCancellationRequested();
            return record != null && record.Id.Value == user && record.IsDeleted != true && !record.IsAccountDisabled;
        }

        private async Task<bool> CanUseOwnerAsync(string owner, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var service = _system.HostUser?.Id;
            if (!_enabled || string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(service) ||
                !_owners.Contains(owner, StringComparer.Ordinal)) return false;
            var member = await _members.GetOrgUserAsync(owner, service);
            ct.ThrowIfCancellationRequested();
            return member != null && member.OrgId.Value == owner && member.UserId.Value == service &&
                await ActiveUserAsync(service, ct);
        }

        private async Task<DevOpsApplication> FindApplicationAsync(string appId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!_enabled || string.IsNullOrWhiteSpace(appId)) return null;
            // Exact canonical ID read only, never a general unscoped management API.
            var app = await _documents.GetClient().GetDocumentAsync<DevOpsApplication>(appId, false);
            ct.ThrowIfCancellationRequested();
            return app != null && app.Id.Value == appId && app.IsDeleted != true &&
                await CanUseOwnerAsync(app.OwnerOrganization?.Id, ct) ? app : null;
        }

        public async Task<string> ResolveOwnerAsync(NotificationCaller caller, CancellationToken ct)
        {
            if (!_enabled || caller == null || caller.PrincipalId != caller.UserId ||
                !await ActiveUserAsync(caller.UserId, ct)) return null;
            return (await FindApplicationAsync(caller.AppId, ct))?.OwnerOrganization?.Id;
        }

        public async Task<DevOpsApplication> GetApplicationAsync(string owner, string appId)
        {
            var app = await FindApplicationAsync(appId, CancellationToken.None);
            return app?.OwnerOrganization?.Id == owner ? app : null;
        }

        public async Task<bool> AuthorizeEndpointAsync(NotificationCaller caller, DeliveryEndpoint endpoint, CancellationToken ct)
        {
            if (caller == null || endpoint == null || endpoint.UserId != caller.UserId ||
                endpoint.AppId != caller.AppId || endpoint.AppInstanceId != caller.AppInstanceId) return false;
            var owner = await ResolveOwnerAsync(caller, ct);
            return owner != null && owner == endpoint.Organization?.Id;
        }

        public async Task<BindingDecision> AuthorizeAsync(NotificationCaller caller, string owner, string appId,
            ConfigurationAccess access, CancellationToken ct)
        {
            if (access != ConfigurationAccess.EndpointRegistration || caller == null || caller.AppId != appId ||
                string.IsNullOrWhiteSpace(owner)) return BindingDecision.Denied;
            return await ResolveOwnerAsync(caller, ct) == owner ? BindingDecision.Allowed : BindingDecision.Denied;
        }

        public async Task<BindingDecision> VerifyConfigurationAsync(NotificationCaller caller, RuntimeProviderConfiguration configuration,
            ConfigurationAccess access, CancellationToken ct)
        {
            if (configuration == null) return BindingDecision.Denied;
            var decision = await AuthorizeAsync(caller, configuration.Organization?.Id, configuration.AppId, access, ct);
            if (decision != BindingDecision.Allowed) return decision;
            // Native providers remain fail-closed; Web Push uses only versions
            // registered by the protected app-owner configuration operation.
            if (!_webPushEnabled || _store == null || configuration.ProviderKey != "webpush")
                return BindingDecision.Unavailable;
            if (!configuration.Enabled) return BindingDecision.Denied;
            return await WebPushConfigurationBinding.VerifyAsync(_store, configuration, ct);
        }

        public Task<BindingDecision> VerifyEndpointAsync(NotificationCaller worker, string origin, DeliveryEndpoint endpoint, CancellationToken ct)
        { ct.ThrowIfCancellationRequested(); return Task.FromResult(BindingDecision.Denied); }
    }
}
