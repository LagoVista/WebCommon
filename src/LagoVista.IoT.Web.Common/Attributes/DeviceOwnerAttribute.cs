// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: bd8746eb9b3557038085aa7bfa56a5bfa9048acda3094748930931e76bb3ee4b
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.Core.Validation;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace LagoVista.IoT.Web.Common.Attributes
{
    public class DeviceOwnerAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            if (context.HttpContext.User != null && context.HttpContext.User.Identity.IsAuthenticated)
            {
                if (context.HttpContext.User.Claims.Single(clm => clm.Type == ClaimsFactory.Logintype).Value != nameof(DeviceOwnerUser))
                {
                    context.HttpContext.Response.StatusCode = 403;
                    context.HttpContext.Response.Headers.Clear();
                    var result = new InvokeResult();
                    result.Errors.Add(new ErrorMessage("Requires device owner user login"));
                    context.Result = new JsonResult(result);
                }
            }
        }
    }
}
 