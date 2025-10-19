// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 08de8ad8ac1374e0cda3fa8969ace78131334ef0abc806e4f9316589fcc75a8e
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.Core.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace LagoVista.IoT.Web.Common.Attributes
{
    public class OrgAdminAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            if (context.HttpContext.User != null && context.HttpContext.User.Identity.IsAuthenticated)
            {
         /*       if (!context.HttpContext.User.HasClaim(ClaimsFactory.IsOrgAdmin, true.ToString()))
                {
                    context.HttpContext.Response.StatusCode = 403;
                    context.HttpContext.Response.Headers.Clear();
                    var result = new InvokeResult();
                    result.Errors.Add(new ErrorMessage("Action Requires Org Admin Role"));
                    context.Result = new JsonResult(result);
                }*/
            }
            /* else - Can't think of where the Authorize attribute wouldn't take care of this case */
        }
    }
}

