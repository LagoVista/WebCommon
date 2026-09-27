using LagoVista.Core.Security;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LagoVista.Web.Common.Security
{
    public sealed class RuntimeSignedRequestSessionService : IRuntimeSignedRequestSessionService
    {
        private sealed class Session
        {
            public string Secret { get; set; }
            public string InstanceId { get; set; }
            public string OrganizationId { get; set; }
            public string UserId { get; set; }
            public string HostId { get; set; }
            public string SourceIp { get; set; }
            public DateTime ExpiresUtc { get; set; }
        }

        private readonly ConcurrentDictionary<string, Session> _sessions =
            new ConcurrentDictionary<string, Session>(StringComparer.Ordinal);

        public RuntimeSignedRequestSessionValidationResult TryValidate(HttpRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var sessionId = ReadHeader(request, SignedRequestHeaders.RuntimeSessionId);
            var sessionSignature = ReadHeader(request, SignedRequestHeaders.RuntimeSessionSignature);
            if (String.IsNullOrWhiteSpace(sessionId) || String.IsNullOrWhiteSpace(sessionSignature))
            {
                return new RuntimeSignedRequestSessionValidationResult();
            }

            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return new RuntimeSignedRequestSessionValidationResult();
            }

            var now = DateTime.UtcNow;
            if (session.ExpiresUtc <= now)
            {
                _sessions.TryRemove(sessionId, out _);
                return new RuntimeSignedRequestSessionValidationResult();
            }

            if (!String.Equals(session.SourceIp, ResolveSourceIp(request), StringComparison.Ordinal) ||
                !String.Equals(session.InstanceId, ReadHeader(request, SignedRequestHeaders.InstanceId), StringComparison.Ordinal) ||
                !String.Equals(session.OrganizationId, ReadHeader(request, SignedRequestHeaders.OrganizationId), StringComparison.Ordinal) ||
                !String.Equals(session.UserId, ReadHeader(request, SignedRequestHeaders.UserId), StringComparison.Ordinal))
            {
                return new RuntimeSignedRequestSessionValidationResult();
            }

            var headers = ReadHeaders(request);
            var context = new SignedRequestCanonicalContext
            {
                Profile = SignedRequestCanonicalProfile.RuntimeInstanceHttpV1,
                Headers = headers,
                Method = request.Method,
                PathAndQuery = $"{request.Path}{request.QueryString}",
                BodySha256 = ReadHeader(request, SignedRequestHeaders.BodySha256)
            };

            if (!RuntimeSignedRequestSessionSigner.Validate(
                session.Secret,
                sessionId,
                sessionSignature,
                context))
            {
                return new RuntimeSignedRequestSessionValidationResult();
            }

            return new RuntimeSignedRequestSessionValidationResult
            {
                Successful = true,
                InstanceId = session.InstanceId,
                OrganizationId = session.OrganizationId,
                UserId = session.UserId,
                HostId = session.HostId,
                ExpiresUtc = session.ExpiresUtc
            };
        }

        public void Issue(HttpRequest request, HttpResponse response, string hostId, TimeSpan ttl)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (response == null) throw new ArgumentNullException(nameof(response));
            if (ttl <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ttl));

            var sessionId = RuntimeSignedRequestSessionSigner.CreateSessionId();
            var secret = RuntimeSignedRequestSessionSigner.CreateSecret();
            var expiresUtc = DateTime.UtcNow.Add(ttl);

            var session = new Session
            {
                Secret = secret,
                InstanceId = ReadRequiredHeader(request, SignedRequestHeaders.InstanceId),
                OrganizationId = ReadRequiredHeader(request, SignedRequestHeaders.OrganizationId),
                UserId = ReadRequiredHeader(request, SignedRequestHeaders.UserId),
                HostId = hostId ?? String.Empty,
                SourceIp = ResolveSourceIp(request),
                ExpiresUtc = expiresUtc
            };

            _sessions[sessionId] = session;

            response.Headers[SignedRequestHeaders.RuntimeSessionId] = sessionId;
            response.Headers[SignedRequestHeaders.RuntimeSessionSecret] = secret;
            response.Headers[SignedRequestHeaders.RuntimeSessionExpiresUtc] = expiresUtc.ToString("o");
        }

        private static Dictionary<string, string> ReadHeaders(HttpRequest request)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in request.Headers)
            {
                headers[header.Key] = header.Value.ToString();
            }

            return headers;
        }

        private static string ReadRequiredHeader(HttpRequest request, string name)
        {
            var value = ReadHeader(request, name);
            if (String.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Missing required runtime session header source: {name}");
            }

            return value;
        }

        private static string ReadHeader(HttpRequest request, string name)
        {
            return request.Headers.TryGetValue(name, out var value) ? value.ToString() : String.Empty;
        }

        private static string ResolveSourceIp(HttpRequest request)
        {
            return request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? String.Empty;
        }
    }
}
