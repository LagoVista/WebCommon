using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using LagoVista.AspNetCore.Identity.Interfaces;
using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.AspNetCore.Identity.Services;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.DevOps.Models;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.IoT.Web.Common.Notifications;
using LagoVista.Notifications.Contracts;
using LagoVista.Notifications.Execution;
using LagoVista.Notifications.Models;
using LagoVista.UserAdmin.Interfaces.Repos.Apps;
using LagoVista.UserAdmin.Interfaces.Repos.Users;
using LagoVista.UserAdmin.Managers;
using LagoVista.UserAdmin.Models.Apps;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LagoVista.IoT.Web.Common.Tests;

[TestClass]
public sealed class BrowserNotificationRegistrationTests
{
    private const string User = "11111111111111111111111111111111";
    private const string App = "22222222222222222222222222222222";
    private const string Owner = "33333333333333333333333333333333";
    private const string Customer = "44444444444444444444444444444444";
    private const string Environment = "dev";
    private static readonly Uri Origin = new Uri("https://portal.example.com");
    private static readonly CancellationToken Ct = CancellationToken.None;

    [TestMethod]
    public async Task ActualProtectedApplicationCookieAndCanonicalInstanceReachExistingEndpointService()
    {
        using var fixture = new Fixture();
        await fixture.LoginAsync();
        using var scope = fixture.Provider.CreateScope();
        var context = fixture.Request(scope.ServiceProvider);
        var service = fixture.Service(scope.ServiceProvider);
        var config = await service.ConfigurationAsync(context, Ct);
        Assert.IsTrue(config.Successful);
        Assert.AreEqual(App, config.Result.AppId);
        Assert.AreEqual(Origin.GetLeftPart(UriPartial.Authority), config.Result.Origin);
        Assert.AreEqual(Environment, config.Result.Environment);

        var list = await service.ListAsync(context, Ct);
        Assert.IsTrue(list.Successful);
        Assert.AreEqual(1, list.Model.Count());
        Assert.AreEqual(fixture.Instance.RowKey, list.Model.Single().AppInstanceId);
        var registration = await service.RegisterAsync(context, new EndpointRegistrationRequest
        {
            ProviderKey = "webpush", Environment = Environment,
            ProviderConfigurationId = config.Result.ConfigurationId,
            Consented = true, ExpectedETag = "v1"
        }, Ct);
        Assert.IsTrue(registration.Successful);
        fixture.Endpoints.Verify(e => e.EnrollOrRefreshAsync(It.Is<NotificationCaller>(c =>
            c.UserId == User && c.AppId == App && c.AppInstanceId == fixture.Instance.RowKey && c.OrganizationId == Customer),
            It.Is<EndpointEnrollment>(r => r.ExpectedETag == "v1"), Ct), Times.Once);
        var endpoint = list.Model.Single();
        Assert.IsTrue((await service.UnregisterAsync(context, endpoint.Id, 1, "v1", Ct)).Successful);
        fixture.Endpoints.Verify(e => e.UnregisterAsync(It.Is<NotificationCaller>(c => c.UserId == User),
            endpoint.Id, 1, "v1", Ct), Times.Once);
    }

    [DataTestMethod]
    [DataRow("no-cookie")]
    [DataRow("missing-continuity")]
    [DataRow("wrong-origin")]
    [DataRow("wrong-provider")]
    [DataRow("wrong-configuration")]
    public async Task RejectsUnboundOrWrongScopeBeforeEndpointMutation(string scenario)
    {
        using var fixture = new Fixture();
        await fixture.LoginAsync();
        using var scope = fixture.Provider.CreateScope();
        var context = fixture.Request(scope.ServiceProvider, scenario == "no-cookie" ? "" :
            scenario == "missing-continuity" ? fixture.AuthCookie : fixture.Cookies);
        if (scenario == "no-cookie")
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimsFactory.CurrentUserId, User),
                new Claim(ClaimsFactory.AppInstanceId, fixture.Instance.RowKey)
            }, "Bearer"));
        if (scenario == "wrong-origin") context.Request.Headers.Origin = "https://other.example.com";
        var service = fixture.Service(scope.ServiceProvider);
        var result = await service.RegisterAsync(context, new EndpointRegistrationRequest
        {
            ProviderKey = scenario == "wrong-provider" ? "apns" : "webpush", Environment = Environment,
            ProviderConfigurationId = scenario == "wrong-configuration" ? new NormalizedId32(Owner) : fixture.Config.Id,
            Consented = true
        }, Ct);
        Assert.IsFalse(result.Successful);
        fixture.Endpoints.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task DisabledModuleUsesOrdinaryInvokeResultWithoutResolvingDependencies()
    {
        var services = new ServiceCollection();
        LagoVista.IoT.Web.Common.Startup.ConfigureServices(services, new ConfigurationBuilder().Build());
        Assert.IsFalse(services.Any(s => s.ServiceType == typeof(BrowserNotificationRegistration)));
        using var provider = new ServiceCollection().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        var controller = new BrowserNotificationEndpointsController
            { ControllerContext = new ControllerContext { HttpContext = context } };
        var result = await controller.Configuration(Ct);
        Assert.IsFalse(result.Successful);
        Assert.AreEqual("browser-notifications-disabled", result.Errors.Single().ErrorCode);
    }

    private sealed class Fixture : IDisposable
    {
        public readonly ServiceProvider Provider;
        public readonly Mock<INotificationEndpointService> Endpoints = new(MockBehavior.Strict);
        public readonly RuntimeProviderConfiguration Config;
        public AppInstance Instance;
        public string Cookies;
        public string AuthCookie;
        private readonly BrowserInstallationCookie _cookie;
        private readonly BrowserInstallationSession _session;
        private readonly Mock<IAppInstanceRepo> _instances = new(MockBehavior.Strict);
        private readonly Mock<IAppUserRepo> _users = new(MockBehavior.Strict);
        private readonly Mock<IBrowserInstallationApplicationResolver> _resolver = new(MockBehavior.Strict);
        private readonly AppUser _user = new() { Id = new NormalizedId32(User), LoginType = LoginTypes.AppUser };
        private readonly WebPushConfigurationDiscovery _discovery;

        public Fixture()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddAuthentication(IdentityConstants.ApplicationScheme).AddCookie(IdentityConstants.ApplicationScheme,
                options => options.Cookie.Name = "fixture-auth");
            Provider = services.BuildServiceProvider();
            _cookie = new BrowserInstallationCookie(Provider.GetRequiredService<IDataProtectionProvider>());
            _session = new BrowserInstallationSession(_cookie, new BrowserInstallationManager(_instances.Object));
            _instances.Setup(r => r.GetAppInstanceAsync(User, It.IsAny<string>()))
                .ReturnsAsync((string user, string id) => Instance?.RowKey == id ? Instance : null);
            _instances.Setup(r => r.AddAppInstanceAsync(It.IsAny<AppInstance>()))
                .Callback<AppInstance>(r => Instance = r).Returns(Task.CompletedTask);
            _users.Setup(r => r.FindByIdAsync(User)).ReturnsAsync(_user);
            _resolver.Setup(r => r.ResolveAsync(It.IsAny<HttpContext>())).ReturnsAsync((App, Origin));

            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var point = key.ExportParameters(false).Q;
            var publicKey = Convert.ToBase64String(new byte[] { 4 }.Concat(point.X).Concat(point.Y).ToArray())
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            Config = new RuntimeProviderConfiguration
            {
                Id = RuntimeProviderConfiguration.CreateId(Owner, App, "webpush", Environment, "v1"),
                Organization = EntityHeader.Create(Owner, "Owner"), AppId = App, ProviderKey = "webpush",
                Channel = NotificationChannel.WebPush, Environment = Environment, CredentialVersion = "v1",
                CredentialPurpose = WebPushConfigurationBinding.Purpose, CredentialSecretId = Guid.NewGuid().ToString("N"),
                Enabled = true, ETag = "cfg1",
                Vapid = new VapidRuntimeSettings { Origin = Origin.GetLeftPart(UriPartial.Authority),
                    Subject = "mailto:push@example.com", PublicKey = publicKey }
            };
            var store = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            store.Setup(s => s.QueryAsync(It.IsAny<StorageQuery<RuntimeProviderConfiguration>>(), Ct))
                .ReturnsAsync(new StoragePageResult<RuntimeProviderConfiguration>(new[] { Config }));
            var policy = new Mock<INotificationConfigurationAuthority>(MockBehavior.Strict);
            policy.Setup(p => p.AuthorizeAsync(It.IsAny<NotificationCaller>(), Owner, App, ConfigurationAccess.EndpointRegistration, Ct))
                .ReturnsAsync(BindingDecision.Allowed);
            policy.Setup(p => p.VerifyConfigurationAsync(It.IsAny<NotificationCaller>(), Config, ConfigurationAccess.EndpointRegistration, Ct))
                .ReturnsAsync(BindingDecision.Allowed);
            var authority = new NotificationInstallationAuthority((_, _) => Task.FromResult(Owner), _instances.Object.GetAppInstanceAsync,
                (_, _) => Task.FromResult(new DevOpsApplication { Id = new NormalizedId32(App), OwnerOrganization = EntityHeader.Create(Owner, "Owner") }),
                (_, _, _) => Task.FromResult(true), (_, _, _) => Task.FromResult(false));
            var providers = new NotificationProviderRegistry(new INotificationProvider[]
            {
                new WebPushNotificationProvider((_, _) => Task.FromResult(new WebPushTransportResponse { RejectedBeforeSend = true }))
            });
            _discovery = new WebPushConfigurationDiscovery(store.Object, authority, policy.Object, providers);
        }

        public async Task LoginAsync()
        {
            using var scope = Provider.CreateScope();
            var context = Request(scope.ServiceProvider, "");
            _cookie.Initialize(context, Origin);
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, User),
                new Claim(ClaimsFactory.CurrentUserId, User),
                new Claim(ClaimsFactory.CurrentOrgId, Customer)
            }, IdentityConstants.ApplicationScheme));
            await _session.BindProvenSessionAsync(context, principal, _user, App, Origin);
            await context.SignInAsync(IdentityConstants.ApplicationScheme, principal);
            var pairs = context.Response.Headers.SetCookie.Select(v => v.Split(';')[0]).ToArray();
            Cookies = string.Join("; ", pairs);
            AuthCookie = pairs.Single(p => p.StartsWith("fixture-auth=", StringComparison.Ordinal));
            var status = new EndpointStatus
            {
                Id = new NormalizedId32("55555555555555555555555555555555"), AppId = App,
                AppInstanceId = Instance.RowKey, ProviderKey = "webpush", Environment = Environment,
                Generation = 1, ETag = "v1", Enabled = true, Consented = true
            };
            Endpoints.Setup(e => e.ListAsync(It.IsAny<NotificationCaller>(), App, Ct))
                .ReturnsAsync(InvokeResult<IReadOnlyList<EndpointStatus>>.Create(new[] { status }));
            Endpoints.Setup(e => e.EnrollOrRefreshAsync(It.IsAny<NotificationCaller>(), It.IsAny<EndpointEnrollment>(), Ct))
                .ReturnsAsync(InvokeResult<EndpointStatus>.Create(status));
            Endpoints.Setup(e => e.UnregisterAsync(It.IsAny<NotificationCaller>(), status.Id, 1, "v1", Ct))
                .ReturnsAsync(InvokeResult.Success);
        }

        public DefaultHttpContext Request(IServiceProvider services, string cookies = null)
        {
            var context = new DefaultHttpContext { RequestServices = services };
            context.Request.Scheme = "https";
            context.Request.Host = new HostString(Origin.Host);
            context.Request.Headers.Origin = Origin.GetLeftPart(UriPartial.Authority);
            context.Request.Headers.Cookie = cookies ?? Cookies ?? "";
            return context;
        }

        public BrowserNotificationRegistration Service(IServiceProvider services) => new(_resolver.Object, _session,
            services.GetRequiredService<IAuthenticationSchemeProvider>(), _users.Object, Endpoints.Object, _discovery, Environment);
        public void Dispose() => Provider.Dispose();
    }
}
