using LagoVista.Core.Security;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace LagoVista.Web.Common.Security
{
    public class RequireSignedRequestFilter : IAsyncAuthorizationFilter
    {
        private readonly ISignedRequestHttpValidator _validator;
        private readonly SignedRequestCanonicalProfile _profile;
        private readonly IAdminLogger _logger;

        public RequireSignedRequestFilter(ISignedRequestHttpValidator validator, IAdminLogger adminLogger, SignedRequestCanonicalProfile profile = SignedRequestCanonicalProfile.ServiceHttpV1)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _profile = profile;
            _logger = adminLogger ?? throw new ArgumentNullException( nameof(adminLogger));
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var http = context.HttpContext;
            if (SignedMachineAuthentication.IsSharedEndpoint(http) &&
                SignedMachineAuthentication.ClaimsMachineNamespace(http, http.RequestServices.GetServices<ISignedMachineKeyProvider>()))
            {
                // Reuse the request handler's cached result, never revalidate/claim replay or
                // fall through to legacy signing for a reserved identity.
                var machine = await http.AuthenticateAsync(SignedMachineAuthentication.Scheme);
                if (!machine.Succeeded)
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                var organization = machine.Principal.FindFirst(SignedMachineAuthentication.OrganizationClaim)?.Value;
                if (String.IsNullOrWhiteSpace(organization) ||
                    !String.Equals(http.Request.RouteValues["organizationId"] as string, organization, StringComparison.Ordinal))
                {
                    context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                    return;
                }

                http.User = machine.Principal;
                // Controller route binding now uses the verified registration organization.
                http.Request.RouteValues["organizationId"] = organization;
                return;
            }

            SignedRequestValidationResult result;


            switch (_profile)
            {
                case SignedRequestCanonicalProfile.ServiceHttpV1:
                    result = await _validator.ValidateServiceHttpV1Async(context.HttpContext.Request, context.HttpContext.RequestAborted);
                    break;

                default:
                    context.Result = new UnauthorizedObjectResult($"Signed request profile '{_profile}' is not supported by this filter.");
                    return;
            }

            if (!result.Successful)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    errorCode = result.ErrorCode,
                    errorMessage = result.ErrorMessage
                });
            }
        }
    }
}
