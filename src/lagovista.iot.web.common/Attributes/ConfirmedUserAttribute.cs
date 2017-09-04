using LagoVista.AspNetCore.Identity;
using LagoVista.AspNetCore.Identity.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;

namespace LagoVista.IoT.Web.Common.Attributes
{
    public class ConfirmedUserAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            var ctrlDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (ctrlDescriptor != null && (ctrlDescriptor.ControllerName == "Account" && ctrlDescriptor.ActionName == "LogOff"))
            {
                return;
            }

            if ((((string)context.RouteData.Values["controller"]).ToLower() == "verifyidentity") ||
                context.HttpContext.Request.Path.StartsWithSegments(new PathString("/api/verify")) ||
                context.HttpContext.Request.Path.StartsWithSegments(new PathString("/api/org/namespace")) ||
                context.HttpContext.Request.Path.Value.ToLower() == "/api/org")
            {
                return;
            }

            if (context.HttpContext.User != null && context.HttpContext.User.Identity.IsAuthenticated)
            {
                if (context.HttpContext.User.HasClaim(ClaimsFactory.EmailVerified, true.ToString()) && context.HttpContext.User.HasClaim(ClaimsFactory.PhoneVerfiied, true.ToString()))
                {
                    var orgId = context.HttpContext.User.Claims.Where(claim => claim.Type == ClaimsFactory.CurrentOrgId).FirstOrDefault();
                    if ((((string)context.RouteData.Values["controller"]).ToLower() != "verifyidentity") &&
                            (orgId == null || String.IsNullOrEmpty(orgId.Value) || orgId.Value == ClaimsFactory.None) &&
                            !((ctrlDescriptor.ControllerName == "Organization" && ctrlDescriptor.ActionName == "Create")))
                    {
                        context.Result = new RedirectToActionResult("Create", "Organization", null);
                    }
                }
            }
        }
    }
}
