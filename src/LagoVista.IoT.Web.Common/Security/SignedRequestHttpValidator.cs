using LagoVista.Core.Security;
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

        public SignedRequestHttpValidator(ISignedRequestValidatorService validatorService)
        {
            _validatorService = validatorService ?? throw new ArgumentNullException(nameof(validatorService));
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

        public Task<SignedRequestValidationResult> ValidateServiceHttpV1Async(HttpRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return _validatorService.ValidateServiceHttpV1Async(new SignedRequestValidationContext
            {
                Profile = SignedRequestCanonicalProfile.ServiceHttpV1,
                Headers = ReadHeaders(request),
                Method = request.Method,
                PathAndQuery = GetPathAndQuery(request),
                BodySha256 = GetBodySha256Header(request),
                ValidateTimestamp = true,
                MaxClockSkew = TimeSpan.FromMinutes(5)
            }, cancellationToken);
        }

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
