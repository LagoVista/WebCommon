using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.Core.Validation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace LagoVista.IoT.Web.Common.Attributes
{
    public class RequireFreshMfaAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            if (context.HttpContext.User == null || context.HttpContext.User.Identity == null || !context.HttpContext.User.Identity.IsAuthenticated) return;

            var requireMfa = GetBoolClaim(context, ClaimsFactory.OrgRequireMfa);
            var twoFactorEnabled = GetBoolClaim(context, ClaimsFactory.TwoFactorEnabled);

            if (requireMfa && !twoFactorEnabled)
            {
                Deny(context, "enroll_required", "MFA enrollment required.");
                return;
            }

            var freshWindowMinutes = GetIntClaim(context, ClaimsFactory.OrgMfaFreshWindowMinutes, 15);
            var lastMfaUtc = GetStringClaim(context, ClaimsFactory.LastMfaDateTimeUtc);

            if (String.IsNullOrEmpty(lastMfaUtc) || lastMfaUtc == ClaimsFactory.None)
            {
                Deny(context, "step_up_required", "Fresh MFA required.");
                return;
            }

            if (!DateTime.TryParse(lastMfaUtc, out var lastMfaDt))
            {
                Deny(context, "step_up_required", "Fresh MFA required.");
                return;
            }

            var age = DateTime.UtcNow - lastMfaDt.ToUniversalTime();
            if (age.TotalMinutes > freshWindowMinutes)
            {
                Deny(context, "step_up_required", "Fresh MFA required.");
                return;
            }
        }

        private static bool GetBoolClaim(ActionExecutedContext context, string claimType)
        {
            var value = context.HttpContext.User.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
            return String.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetIntClaim(ActionExecutedContext context, string claimType, int defaultValue)
        {
            var value = context.HttpContext.User.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
            return Int32.TryParse(value, out var parsed) ? parsed : defaultValue;
        }

        private static string GetStringClaim(ActionExecutedContext context, string claimType)
        {
            return context.HttpContext.User.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        }

        private static void Deny(ActionExecutedContext context, string code, string message)
        {
            context.HttpContext.Response.StatusCode = 403;
            context.HttpContext.Response.Headers.Clear();

            var result = new InvokeResult();
            result.Errors.Add(new ErrorMessage($"{code}:{message}"));

            context.Result = new JsonResult(result);
        }
    }
}
