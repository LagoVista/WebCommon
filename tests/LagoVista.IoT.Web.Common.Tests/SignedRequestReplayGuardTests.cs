using LagoVista.Core.Interfaces;
using LagoVista.Core.Security;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.Web.Common.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Tests
{
    [TestClass]
    public class SignedRequestReplayGuardTests
    {
        [TestMethod]
        public async Task ConcurrentReplicasClaimExactlyOnceAndScopeByCaller()
        {
            var claims = new ConcurrentDictionary<string, byte>();
            var cache = new Mock<ICacheProvider>(MockBehavior.Strict);
            cache.Setup(x => x.AttemptAcquireLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
                .Returns((string key, string token, TimeSpan? ttl) =>
                {
                    Assert.AreEqual(TimeSpan.FromMinutes(11), ttl.Value);
                    Assert.IsFalse(key.Contains("caller-A"));
                    Assert.IsFalse(key.Contains("request-1"));
                    return Task.FromResult(claims.TryAdd(key, 0));
                });
            var first = new SignedRequestReplayGuard(cache.Object, Mock.Of<IAdminLogger>());
            var second = new SignedRequestReplayGuard(cache.Object, Mock.Of<IAdminLogger>());
            var request = SignedRequestValidationResult.Success("request-1", "key");
            var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(i =>
                Task.Run(() => (i % 2 == 0 ? first : second).ClaimVerifiedRequestAsync("caller-A", request))));
            Assert.AreEqual(1, results.Count(x => x.Successful));
            Assert.AreEqual(19, results.Count(x => x.ErrorCode == "signed_request_claim_rejected"));
            Assert.IsTrue((await second.ClaimVerifiedRequestAsync("caller-B", request)).Successful);
            Assert.IsTrue((await first.ClaimVerifiedRequestAsync("caller-A",
                SignedRequestValidationResult.Success("request-2", "key"))).Successful);
            cache.Verify(x => x.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task UnverifiedCancelledAndUnavailableClaimsFailClosed()
        {
            var cache = new Mock<ICacheProvider>();
            cache.Setup(x => x.AttemptAcquireLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
                .ThrowsAsync(new InvalidOperationException("unavailable"));
            var guard = new SignedRequestReplayGuard(cache.Object, Mock.Of<IAdminLogger>());
            Assert.IsFalse((await guard.ClaimVerifiedRequestAsync("caller",
                SignedRequestValidationResult.FromError("invalid_signature", "bad"))).Successful);
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            var request = SignedRequestValidationResult.Success("request", "key");
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() =>
                guard.ClaimVerifiedRequestAsync("caller", request, cancelled.Token));
            cache.Verify(x => x.AttemptAcquireLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Never);
            Assert.AreEqual("signed_request_claim_unavailable", (await guard.ClaimVerifiedRequestAsync("caller", request)).ErrorCode);
        }
    }
}
