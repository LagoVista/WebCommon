using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using LagoVista.Core.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LagoVista.Web.Common.Security
{
    // A provider resolves an existing registered machine; it is not an identity store.
    public interface ISignedMachineKeyProvider
    {
        bool HandlesCaller(string callerId);
        Task<SignedMachineKey> ResolveAsync(string callerId, string keyId, string algorithm, CancellationToken cancellationToken);
    }

    public sealed record SignedMachineKey(string CallerId, string OrganizationId, string MachineId, long Generation,
        SignedRequestValidationKey ValidationKey);

    // Explicit opt-in. Do not use human role or organization claims for machines.
    public sealed class RequireSignedMachineAttribute : AuthorizeAttribute
    {
        public RequireSignedMachineAttribute() => AuthenticationSchemes = SignedMachineAuthentication.Scheme;
    }

    // Opt in individual existing signed engineering actions, not an authorization scheme
    // for legacy callers. The signed-request filter enforces the verified organization.
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public sealed class AllowEnrolledMachineAttribute : Attribute { }

    public static class SignedMachineAuthentication
    {
        public const string Scheme = "SignedMachine";
        public const string OrganizationClaim = "signed-machine:organization";
        public const string MachineClaim = "signed-machine:identity";
        public const string GenerationClaim = "signed-machine:generation";

        internal static bool IsSharedEndpoint(HttpContext context) =>
            context.GetEndpoint()?.Metadata.GetMetadata<AllowEnrolledMachineAttribute>() != null &&
            context.GetEndpoint()?.Metadata.GetMetadata<RequireSignedRequestAttribute>() != null &&
            context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() == null;

        internal static bool ClaimsMachineNamespace(HttpContext context, IEnumerable<ISignedMachineKeyProvider> providers) =>
            context.Request.Headers[SignedRequestHeaders.AppKey]
                .SelectMany(value => (value ?? String.Empty).Split(','))
                .Any(value => providers.Any(provider => provider.HandlesCaller(value.Trim())));

        // Called by the owning library's existing host registration; no new host default.
        public static IServiceCollection AddSignedMachineAuthentication(this IServiceCollection services)
        {
            services.TryAddTransient<SignedRequestHttpValidator>();
            services.TryAddScoped<SignedRequestReplayGuard>();
            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, SignedMachineAuthenticationHandler>(Scheme, _ => { });
            return services;
        }
    }

    public sealed class SignedMachineAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>, IAuthenticationRequestHandler
    {
        private readonly ISignedMachineKeyProvider[] _providers;
        private readonly SignedRequestHttpValidator _validator;
        private readonly SignedRequestReplayGuard _replay;

        public SignedMachineAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
            UrlEncoder encoder, IEnumerable<ISignedMachineKeyProvider> providers, SignedRequestHttpValidator validator,
            SignedRequestReplayGuard replay) : base(options, logger, encoder)
        {
            _providers = providers.ToArray();
            _validator = validator;
            _replay = replay;
        }

        private bool IsMachineEndpoint => Context.GetEndpoint()?.Metadata.GetMetadata<RequireSignedMachineAttribute>() != null &&
            Context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() == null;

        private bool AcceptsMachine => IsMachineEndpoint || SignedMachineAuthentication.IsSharedEndpoint(Context);
        private bool ClaimsMachineNamespace => SignedMachineAuthentication.ClaimsMachineNamespace(Context, _providers);

        // ASP.NET calls request handlers before its ordinary default authentication.
        // A reserved caller may not fall through to an unrelated signed, human or anonymous API action.
        public async Task<bool> HandleRequestAsync()
        {
            if (!ClaimsMachineNamespace && !IsMachineEndpoint) return false;
            if (!AcceptsMachine)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return true;
            }

            // AuthenticateAsync caches the handler result for this HTTP request.
            // The endpoint's explicit scheme will reuse it rather than claim replay twice.
            var authentication = await AuthenticateAsync();
            if (authentication.Succeeded) return false;
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return true;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!AcceptsMachine) return AuthenticateResult.Fail("Machine endpoint authorization required.");
            if (!TryGetCaller(out var caller)) return AuthenticateResult.Fail("Invalid signed machine caller.");
            var matches = _providers.Where(provider => provider.HandlesCaller(caller)).ToArray();
            if (matches.Length != 1) return AuthenticateResult.Fail("Authoritative machine provider required.");

            var resolver = new RequestKeyResolver(matches[0], caller);
            try
            {
                var verified = await _validator.ValidateServiceHttpV1Async(Request, resolver, Context.RequestAborted);
                var machine = resolver.Resolved;
                if (!verified.Successful || machine == null || verified.MatchedKeyId != machine.ValidationKey.KeyId)
                    return AuthenticateResult.Fail("Signed machine authentication failed.");

                var claimed = await _replay.ClaimVerifiedRequestAsync(machine.CallerId, verified, Context.RequestAborted);
                if (!claimed.Successful) return AuthenticateResult.Fail("Signed machine request rejected.");

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, machine.CallerId),
                    new Claim(SignedMachineAuthentication.OrganizationClaim, machine.OrganizationId),
                    new Claim(SignedMachineAuthentication.MachineClaim, machine.MachineId),
                    new Claim(SignedMachineAuthentication.GenerationClaim, machine.Generation.ToString(System.Globalization.CultureInfo.InvariantCulture))
                };
                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
                return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception)
            {
                Logger.LogWarning("Authoritative machine authentication failed; request denied.");
                return AuthenticateResult.Fail("Signed machine authentication failed.");
            }
        }

        private bool TryGetCaller(out string caller)
        {
            caller = null;
            // Reject duplicate or combined signed headers instead of interpreting ambiguous input.
            foreach (var name in new[] { SignedRequestHeaders.AppKey, SignedRequestHeaders.Authorization,
                SignedRequestHeaders.RequestId, SignedRequestHeaders.SigningKeyId, SignedRequestHeaders.SignatureAlgorithm,
                SignedRequestHeaders.KeyMaterialFormat, SignedRequestHeaders.Date, SignedRequestHeaders.Version,
                SignedRequestHeaders.BodySha256 })
            {
                var values = Request.Headers[name];
                if (values.Count != 1 || String.IsNullOrWhiteSpace(values[0]) || values[0].Contains(',')) return false;
            }
            caller = Request.Headers[SignedRequestHeaders.AppKey][0];
            return true;
        }

        private sealed class RequestKeyResolver : ISignedRequestAuthoritativeKeyResolver
        {
            private readonly ISignedMachineKeyProvider _provider;
            private readonly string _caller;
            public SignedMachineKey Resolved { get; private set; }

            public RequestKeyResolver(ISignedMachineKeyProvider provider, string caller)
            {
                _provider = provider;
                _caller = caller;
            }

            // Always authoritative on this path; a mismatch must not reach shared-key fallback.
            public bool HandlesCaller(string callerId) => true;

            public async Task<SignedRequestValidationKey> ResolveAsync(string callerId, string keyId, string algorithm, CancellationToken cancellationToken)
            {
                if (!String.Equals(_caller, callerId, StringComparison.Ordinal)) return null;
                var candidate = await _provider.ResolveAsync(callerId, keyId, algorithm, cancellationToken);
                if (candidate == null || candidate.ValidationKey == null ||
                    candidate.CallerId != callerId || candidate.ValidationKey.AppKey != callerId ||
                    candidate.ValidationKey.KeyId != keyId || candidate.ValidationKey.Algorithm != algorithm ||
                    String.IsNullOrWhiteSpace(candidate.OrganizationId) || String.IsNullOrWhiteSpace(candidate.MachineId) ||
                    candidate.Generation <= 0) return null;
                Resolved = candidate;
                return candidate.ValidationKey;
            }
        }
    }
}
