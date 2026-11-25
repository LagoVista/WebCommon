using LagoVista.Core.Interfaces;
using LagoVista.Core.Security;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.Web.Common.Security
{
    /// <summary>
    /// Explicit opt-in for an authenticated machine request after signature AND actual body
    /// verification, before dispatch. Not an authentication handler, not enabled on legacy
    /// endpoints, and not operation-level idempotency. The caller identity must come from
    /// authoritative authentication, never directly from request headers.
    /// </summary>
    public sealed class SignedRequestReplayGuard
    {
        private readonly ICacheProvider _cache;
        private readonly IAdminLogger _logger;
        // Five minutes past OR future skew means a request first received at the
        // earliest permitted instant can still be valid ten minutes later.
        // Add one minute of boundary/clock margin. Never release an accepted claim.
        private static readonly TimeSpan Retention = TimeSpan.FromMinutes(11);

        public SignedRequestReplayGuard(ICacheProvider cache, IAdminLogger logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SignedRequestValidationResult> ClaimVerifiedRequestAsync(
            string canonicalCallerId, SignedRequestValidationResult verified, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (String.IsNullOrWhiteSpace(canonicalCallerId) || canonicalCallerId.Length > 512 ||
                verified == null || !verified.Successful || String.IsNullOrWhiteSpace(verified.RequestId) ||
                verified.RequestId.Length > 512)
                return SignedRequestValidationResult.FromError("replay_claim_invalid", "Verified machine identity and request ID required.");

            // Length prefix avoids delimiter collisions. Scope is stable across key rotations;
            // separate callers can use the same request ID. Never put raw IDs in cache keys.
            var material = canonicalCallerId.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                ":" + canonicalCallerId + verified.RequestId;
            var key = "security:signed-request:replay:v1:" +
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
            try
            {
                var acquired = await _cache.AttemptAcquireLockAsync(key, "claimed", Retention).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (acquired) return verified;
                // The existing cache contract returns false for an unavailable/disabled
                // distributed provider too. Both outcomes must deny; do not assert replay.
                _logger.AddError("SignedRequest.Replay", "Distributed request claim rejected (duplicate or unavailable).");
                return SignedRequestValidationResult.FromError("signed_request_claim_rejected", "Signed request could not be claimed.");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception)
            {
                _logger.AddError("SignedRequest.Replay", "Distributed request claim failed; request denied.");
                return SignedRequestValidationResult.FromError("signed_request_claim_unavailable", "Signed request could not be claimed.");
            }
        }
    }
}
