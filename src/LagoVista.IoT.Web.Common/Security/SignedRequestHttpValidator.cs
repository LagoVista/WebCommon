using LagoVista.Core.Security;
using LagoVista.CloudStorage.RuntimeControls;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.Web.Common.Security
{
    public class SignedRequestHttpValidator : ISignedRequestHttpValidator
    {
        private readonly ISignedRequestValidatorService _validatorService;
        private readonly IAdminLogger _adminLogger;
        private readonly IRuntimeSignedRequestSessionService _runtimeSessionService;
        private readonly IRuntimeControlService _runtimeControls;
        // Catalog entry ships in CloudStorage; default-on also covers older package composition.
        private const string VerifyBodyHashControl = "Security.SignedRequests.VerifyBodyHash";

        public SignedRequestHttpValidator(ISignedRequestValidatorService validatorService, IAdminLogger adminLogger, IRuntimeSignedRequestSessionService runtimeSessionService)
            : this(validatorService, adminLogger, runtimeSessionService, null) { }

        public SignedRequestHttpValidator(ISignedRequestValidatorService validatorService, IAdminLogger adminLogger,
            IRuntimeSignedRequestSessionService runtimeSessionService, IRuntimeControlService runtimeControls)
        {
            _validatorService = validatorService ?? throw new ArgumentNullException(nameof(validatorService));
            _adminLogger = adminLogger ?? throw new ArgumentNullException(nameof(adminLogger));
            _runtimeSessionService = runtimeSessionService ?? throw new ArgumentNullException(nameof(runtimeSessionService));
            _runtimeControls = runtimeControls;
        }

        public SignedRequestValidationResult ValidateRuntimeInstanceV1(HttpRequest request, string key1, string key2)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return _validatorService.ValidateRuntimeInstanceV1(new SignedRequestValidationContext
            {
                Profile = SignedRequestCanonicalProfile.RuntimeInstanceV1,
                Headers = ReadHeaders(request),
                Key1 = key1,
                Key2 = key2,
                ValidateTimestamp = true,
                MaxClockSkew = TimeSpan.FromMinutes(5)
            });
        }

        public SignedRequestValidationResult ValidateRuntimeInstanceHttpV1(HttpRequest request, string key1, string key2)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return _validatorService.ValidateRuntimeInstanceHttpV1(new SignedRequestValidationContext
            {
                Profile = SignedRequestCanonicalProfile.RuntimeInstanceHttpV1,
                Headers = ReadHeaders(request),
                Key1 = key1,
                Key2 = key2,
                Method = request.Method,
                PathAndQuery = GetPathAndQuery(request),
                BodySha256 = GetBodySha256Header(request),
                ValidateTimestamp = true,
                MaxClockSkew = TimeSpan.FromMinutes(5)
            });
        }

        public RuntimeSignedRequestSessionValidationResult TryValidateRuntimeSession(HttpRequest request)
        {
            return _runtimeSessionService.TryValidate(request);
        }

        public void IssueRuntimeSession(HttpRequest request, HttpResponse response, string hostId, TimeSpan ttl)
        {
            _runtimeSessionService.Issue(request, response, hostId, ttl);
        }

        public async Task<SignedRequestValidationResult> ValidateServiceHttpV1Async(HttpRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));


            var result = await _validatorService.ValidateServiceHttpV1Async(new SignedRequestValidationContext
            {
                Profile = SignedRequestCanonicalProfile.ServiceHttpV1,
                Headers = ReadHeaders(request),
                Method = request.Method,
                PathAndQuery = GetPathAndQuery(request),
                BodySha256 = GetBodySha256Header(request),
                ValidateTimestamp = true,
                MaxClockSkew = TimeSpan.FromMinutes(5)
            }, cancellationToken).ConfigureAwait(false);

            if (!result.Successful) return result;
            // A missing catalog or runtime service cannot silently turn off verification.
            var controlRegistered = RuntimeControlCatalog.Definitions.Any(x => x.Key == VerifyBodyHashControl);
            if (_runtimeControls != null && controlRegistered && !_runtimeControls.IsEnabled(VerifyBodyHashControl))
                return result;

            var bodyResult = await VerifyBodyAsync(request, cancellationToken).ConfigureAwait(false);
            if (bodyResult != null)
            {
                _adminLogger.AddError("SignedRequest.BodyIntegrity",
                    $"{bodyResult.ErrorCode}; request correlation={request.HttpContext.TraceIdentifier}");
                return bodyResult;
            }

            return result;
        }

        // Returns null on success. Verify bytes, not reserialized JSON, and leave the stream
        // available to the original endpoint. ServiceHttpV1 canonicalization is unchanged.
        internal static async Task<SignedRequestValidationResult> VerifyBodyAsync(HttpRequest request, CancellationToken cancellationToken)
        {
            byte[] expected;
            try
            {
                expected = Convert.FromBase64String(GetBodySha256Header(request));
                if (expected.Length != 32) return BodyMismatch();
            }
            catch (FormatException) { return BodyMismatch(); }

            request.EnableBuffering();
            var originalPosition = request.Body.Position;
            try
            {
                request.Body.Position = 0;
                using var sha = SHA256.Create();
                var actual = await sha.ComputeHashAsync(request.Body, cancellationToken).ConfigureAwait(false);
                return CryptographicOperations.FixedTimeEquals(expected, actual) ? null : BodyMismatch();
            }
            catch (IOException)
            {
                return SignedRequestValidationResult.FromError("signed_body_unreadable", "Signed request body could not be verified.");
            }
            finally
            {
                request.Body.Position = originalPosition;
            }
        }

        private static SignedRequestValidationResult BodyMismatch() =>
            SignedRequestValidationResult.FromError("signed_body_hash_mismatch", "Signed request body hash does not match the received body.");

        private static Dictionary<string, string> ReadHeaders(HttpRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var header in request.Headers)
            {
                headers[header.Key] = header.Value.ToString();
            }

            return headers;
        }

        private static string GetPathAndQuery(HttpRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return $"{request.Path}{request.QueryString}";
        }

        private static string GetBodySha256Header(HttpRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return request.Headers.TryGetValue(SignedRequestHeaders.BodySha256, out var value)
                ? value.ToString()
                : String.Empty;
        }
    }
}
