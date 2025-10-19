// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 9fa8560921d5125a7cd69f61c62b45c5e187aaf3d66db8961e67070ed1ca4929
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.AspNetCore.Identity.Managers;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using LagoVista.Core.Validation;

namespace LagoVista.IoT.Web.Common.Attributes
{
    public class SystemAdminAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            if (context.HttpContext.User != null && context.HttpContext.User.Identity.IsAuthenticated)
            {
                if (!context.HttpContext.User.HasClaim(ClaimsFactory.IsSystemAdmin, true.ToString()))
                {
                    context.HttpContext.Response.StatusCode = 403;
                    context.HttpContext.Response.Headers.Clear();
                    var result = new InvokeResult();
                    result.Errors.Add(new ErrorMessage("Action Requires System Admin Role"));
                    context.Result = new JsonResult(result);
                }
            }
            /* else - Can't think of where the Authorize attribute wouldn't take care of this case */
        }
    }
}
