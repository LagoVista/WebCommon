using LagoVista.Core.Security;
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

        public RequireSignedRequestFilter(ISignedRequestHttpValidator validator, SignedRequestCanonicalProfile profile)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _profile = profile;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

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
