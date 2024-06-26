﻿using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.Core.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace LagoVista.IoT.Web.Common.Attributes
{
    public class AppBuilderAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            if (context.HttpContext.User != null && context.HttpContext.User.Identity.IsAuthenticated)
            {
                /*if (!context.HttpContext.User.HasClaim(ClaimsFactory.IsAppBuilder, true.ToString()))
                {
                    context.HttpContext.Response.StatusCode = 403;
                    context.HttpContext.Response.Headers.Clear();
                    var result = new InvokeResult();
                    result.Errors.Add(new ErrorMessage("Action Requires App Builder privileges"));
                    context.Result = new JsonResult(result);
                }*/
            }
            /* else - Can't think of where the Authorize attribute wouldn't take care of this case */
        }
    }
}

