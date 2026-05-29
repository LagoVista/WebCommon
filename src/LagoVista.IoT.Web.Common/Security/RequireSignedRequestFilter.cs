using LagoVista.Core.Security;
using LagoVista.IoT.Logging.Loggers;
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

            SignedRequestValidationResult result;

            _logger.Trace($"{this.Tag()} - Profile {_profile} - {context.HttpContext.Request.Path}");

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
