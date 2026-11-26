using System;
using System.Linq;
using System.Threading.Tasks;
using LagoVista.AspNetCore.Identity.Interfaces;
using LagoVista.AspNetCore.Identity.Services;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.StorageProviders;
using LagoVista.Core.Interfaces;
using LagoVista.Notifications.Contracts;
using LagoVista.Notifications.Execution;
using LagoVista.Notifications.Models;
using LagoVista.UserAdmin.Interfaces.Repos.Apps;
using LagoVista.UserAdmin.Interfaces.Repos.Orgs;
using LagoVista.UserAdmin.Interfaces.Repos.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.IoT.Web.Common.Notifications
{
    internal static class BrowserNotificationRegistrationStartup
    {
        internal static IServiceCollection AddBrowserNotificationRegistration(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MvcOptions>(options => options.InputFormatters.Insert(0, new NotificationRegistrationInputFormatter()));
            if (configuration == null || !configuration.GetValue<bool>("BrowserInstallation:Enabled") ||
                !configuration.GetValue<bool>("HumanNotifications:EndpointRegistrationEnabled") ||
                !configuration.GetValue<bool>("HumanNotifications:WebPush:Enabled"))
                return services;

            var environment = configuration["HumanNotifications:WebPush:Environment"];
            var owners = configuration.GetSection("HumanNotifications:ApplicationOwnerOrganizationIds")
                .GetChildren().Select(x => x.Value).ToArray();
            if (string.IsNullOrWhiteSpace(environment) || owners.Length == 0 ||
                owners.Length > 1000 || owners.Any(string.IsNullOrWhiteSpace) ||
                owners.Distinct(StringComparer.Ordinal).Count() != owners.Length)
                throw new InvalidOperationException("Browser notification environment and application owners must be configured.");

            services.ConfigureApplicationData<RuntimeProviderConfiguration>(d => d.Index(x => x.AppId));
            services.ConfigureApplicationData<DeliveryEndpoint>(d => d.Index(x => x.AppId));
            services.ConfigureApplicationData<DestinationOwnership>(d => d.Index(x => x.Organization.Id));

            // Keep registration's policy/provider registry local to this service.
            // Do not override the host's existing management or worker registrations.
            services.AddScoped(sp =>
            {
                var store = sp.GetRequiredService<IApplicationDataStore>();
                var system = sp.GetRequiredService<ISystemUsers>();
                var policy = new CanonicalNotificationRegistrationAuthority(true, owners,
                    sp.GetRequiredService<IDocumentStorageClientProvider>(), sp.GetRequiredService<IOrgUserRepo>(),
                    system, store, true);
                var authority = new NotificationInstallationAuthority(policy.ResolveOwnerAsync,
                    sp.GetRequiredService<IAppInstanceRepo>().GetAppInstanceAsync, policy.GetApplicationAsync,
                    policy.AuthorizeEndpointAsync, (_, _, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        return Task.FromResult(false);
                    });
                var providers = new NotificationProviderRegistry(new INotificationProvider[]
                {
                    new WebPushNotificationProvider((request, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        return Task.FromResult(new WebPushTransportResponse { RejectedBeforeSend = true });
                    })
                });
                var endpoints = new NotificationEndpointService(store, authority, policy, providers,
                    sp.GetRequiredService<ISecureStorage>(), system.HostUser);
                return new BrowserNotificationRegistration(sp.GetService<IBrowserInstallationApplicationResolver>(),
                    sp.GetService<BrowserInstallationSession>(), sp.GetRequiredService<IAuthenticationSchemeProvider>(),
                    sp.GetRequiredService<IAppUserRepo>(), endpoints,
                    new WebPushConfigurationDiscovery(store, authority, policy, providers), environment);
            });
            return services;
        }
    }
}
