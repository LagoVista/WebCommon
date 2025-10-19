// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: d9ea3fcb70138492610589c0bb98758a74021acff568bb2c635ebedd8f9e8b97
// IndexVersion: 0
// --- END CODE INDEX META ---
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Text;
using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.Core.Validation;
using Microsoft.AspNetCore.Mvc;

namespace LagoVista.IoT.Web.Common.Attributes
{
    public class NonPreviewUserAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            if (context.HttpContext.User != null && context.HttpContext.User.Identity.IsAuthenticated)
            {
                if (!context.HttpContext.User.HasClaim(ClaimsFactory.IsPreviewUser, false.ToString()))
                {
                    context.HttpContext.Response.StatusCode = 403;
                    context.HttpContext.Response.Headers.Clear();
                    var result = new InvokeResult();
                    result.Errors.Add(new ErrorMessage("Action Requires Org Admin Role"));
                    context.Result = new JsonResult(result);
                }
            }
            /* else - Can't think of where the Authorize attribute wouldn't take care of this case */
        }
    }
}
