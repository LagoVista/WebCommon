// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: fea2c5b6082cf3043376b6c113fdde9f13cc18b3c57158ec09fc59033f4bc384
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.Core.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LagoVista.IoT.Web.Common.Attributes
{
    public class FinanceAdminAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            if (context.HttpContext.User != null && context.HttpContext.User.Identity.IsAuthenticated)
            {
                if (!context.HttpContext.User.HasClaim(ClaimsFactory.IsFinancceAdmin, true.ToString()))
                {
                    context.HttpContext.Response.StatusCode = 403;
                    context.HttpContext.Response.Headers.Clear();
                    var result = new InvokeResult();
                    result.Errors.Add(new ErrorMessage("Action Requires Finance Admin privileges"));
                    context.Result = new JsonResult(result);
                }
            }
            /* else - Can't think of where the Authorize attribute wouldn't take care of this case */
        }
    }
}

