using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Security;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.CloudStorage.RuntimeControls;
using LagoVista.Web.Common.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LagoVista.IoT.Web.Common.Tests
{
    [TestClass]
    public class SignedMachineAuthenticationTests
    {
        [TestMethod]
        public async Task RealSignatureUsesAuthoritativeIdentityAndClaimsReplayOnlyOncePerRequest()
        {
            using var fixture = new Fixture();
            var headers = fixture.Headers();
            using var first = fixture.Services.CreateScope();
            var context = fixture.Request(first.ServiceProvider, headers, true);
            var auth = first.ServiceProvider.GetRequiredService<IAuthenticationHandlerProvider>();
            var handler = await auth.GetHandlerAsync(context, SignedMachineAuthentication.Scheme);
            Assert.IsFalse(await ((IAuthenticationRequestHandler)handler).HandleRequestAsync());
            var result = await context.AuthenticateAsync(SignedMachineAuthentication.Scheme);
            Assert.IsTrue(result.Succeeded, result.Failure?.Message);
            Assert.AreEqual("org-a", result.Principal.FindFirst(SignedMachineAuthentication.OrganizationClaim)?.Value);
            Assert.AreEqual("server-a", result.Principal.FindFirst(SignedMachineAuthentication.MachineClaim)?.Value);
            Assert.AreEqual("3", result.Principal.FindFirst(SignedMachineAuthentication.GenerationClaim)?.Value);
            Assert.AreEqual(1, fixture.Provider.Reads);
            Assert.AreEqual(1, fixture.Claims);
            using var second = fixture.Services.CreateScope();
            var replay = fixture.Request(second.ServiceProvider, headers, true);
            Assert.IsFalse((await replay.AuthenticateAsync(SignedMachineAuthentication.Scheme)).Succeeded);
            Assert.AreEqual(2, fixture.Claims);
        }

        [TestMethod]
        public async Task ReservedCallerDeniedOnUnrelatedEndpointAndBodyRollbackCannotBypassMachineVerification()
        {
            using var fixture = new Fixture();
            var headers = fixture.Headers();
            using var scope = fixture.Services.CreateScope();
            var context = fixture.Request(scope.ServiceProvider, headers, false);
            var handler = await scope.ServiceProvider.GetRequiredService<IAuthenticationHandlerProvider>()
                .GetHandlerAsync(context, SignedMachineAuthentication.Scheme);
            Assert.IsTrue(await ((IAuthenticationRequestHandler)handler).HandleRequestAsync());
            Assert.AreEqual(403, context.Response.StatusCode);
            Assert.AreEqual(0, fixture.Provider.Reads);
            using var alteredScope = fixture.Services.CreateScope();
            var altered = fixture.Request(alteredScope.ServiceProvider, headers, true);
            altered.Request.Body = new MemoryStream(new byte[] { 2 });
            Assert.IsFalse((await altered.AuthenticateAsync(SignedMachineAuthentication.Scheme)).Succeeded);
            Assert.AreEqual(0, fixture.Claims);
        }

        [TestMethod]
        public async Task MissingAuthoritativeKeyAndAlteredMethodNeverFallBackOrClaimReplay()
        {
            using var fixture = new Fixture();
            var headers = fixture.Headers();
            fixture.Provider.Enabled = false;
            using var deniedScope = fixture.Services.CreateScope();
            var denied = fixture.Request(deniedScope.ServiceProvider, headers, true);
            Assert.IsFalse((await denied.AuthenticateAsync(SignedMachineAuthentication.Scheme)).Succeeded);
            fixture.Provider.Enabled = true;
            using var alteredScope = fixture.Services.CreateScope();
            var altered = fixture.Request(alteredScope.ServiceProvider, headers, true);
            altered.Request.Method = "PUT";
            Assert.IsFalse((await altered.AuthenticateAsync(SignedMachineAuthentication.Scheme)).Succeeded);
            Assert.AreEqual(0, fixture.Claims);
            fixture.Refresh.Verify(x => x.RefreshAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        private sealed class ProviderStub : ISignedMachineKeyProvider
        {
            public bool Enabled = true;
            public int Reads;
            public SignedRequestValidationKey Key;
            public bool HandlesCaller(string caller) => caller.StartsWith("machine:", StringComparison.OrdinalIgnoreCase);
            public Task<SignedMachineKey> ResolveAsync(string caller, string key, string algorithm, CancellationToken ct)
            {
                Reads++;
                return Task.FromResult(Enabled && key == Key.KeyId ? new SignedMachineKey(Key.AppKey, "org-a", "server-a", 3, Key) : null);
            }
        }

        private sealed class Fixture : IDisposable
        {
            private readonly RSA _rsa = RSA.Create(2048);
            public ProviderStub Provider { get; }
            public ServiceProvider Services { get; }
            public int Claims;
            public Mock<ISignedRequestPublicKeyRefreshService> Refresh = new(MockBehavior.Strict);

            public Fixture()
            {
                Provider = new ProviderStub
                {
                    Key = new SignedRequestValidationKey
                    {
                        AppKey = "machine:org-a:server-a", KeyId = "active-key",
                        PublicKeyMaterial = SignedRequestRsaXmlSerializer.ToXmlString(_rsa.ExportParameters(false), false)
                    }
                };
                var services = new ServiceCollection();
                services.AddLogging();
                services.AddSingleton<ISignedMachineKeyProvider>(Provider);
                var shared = new SignedRequestPublicKeySet();
                services.AddSingleton<ISignedRequestValidatorService>(new SignedRequestValidatorService(new SignedRequestPublicKeySetResolver(shared), Refresh.Object));
                services.AddSingleton(Mock.Of<IAdminLogger>());
                services.AddSingleton(Mock.Of<IRuntimeSignedRequestSessionService>());
                var controls = new Mock<IRuntimeControlService>();
                controls.Setup(x => x.IsEnabled(It.IsAny<string>())).Returns(false);
                services.AddSingleton(controls.Object);
                var cache = new Mock<ICacheProvider>();
                cache.Setup(x => x.AttemptAcquireLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                    .ReturnsAsync(() => ++Claims == 1);
                services.AddSingleton(cache.Object);
                services.AddSignedMachineAuthentication();
                Services = services.BuildServiceProvider();
            }

            public System.Collections.Generic.IDictionary<string, string> Headers() => new SignedRequestHeaderBuilder().BuildHeaders(new SignedRequestHeaderBuildContext
            {
                Profile = SignedRequestCanonicalProfile.ServiceHttpV1,
                AppKey = Provider.Key.AppKey, RequestId = Guid.NewGuid().ToString("N"), DateUtc = DateTimeOffset.UtcNow,
                Version = "1", SigningKeyId = Provider.Key.KeyId,
                PrivateKeyMaterial = SignedRequestRsaXmlSerializer.ToXmlString(_rsa.ExportParameters(true), true),
                Method = "POST", PathAndQuery = "/api/test", Body = new byte[] { 1 }
            });

            public DefaultHttpContext Request(IServiceProvider services, System.Collections.Generic.IDictionary<string, string> headers, bool optedIn)
            {
                var context = new DefaultHttpContext { RequestServices = services };
                context.Request.Method = "POST";
                context.Request.Path = "/api/test";
                context.Request.Body = new MemoryStream(new byte[] { 1 });
                foreach (var header in headers) context.Request.Headers[header.Key] = header.Value;
                context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
                    optedIn ? new EndpointMetadataCollection(new RequireSignedMachineAttribute()) : new EndpointMetadataCollection(), "test"));
                return context;
            }

            public void Dispose()
            {
                Services.Dispose();
                _rsa.Dispose();
            }
        }
    }
}
