using LagoVista.IoT.Web.Common.Claims;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LagoVista.IoT.Web.Common.Attributes
{
    public class SystemAdminAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            var ctrlDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (ctrlDescriptor != null &&
                ctrlDescriptor.ControllerName == "Account" &&
                ctrlDescriptor.ActionName == "LogOff")
            {
                return;
            }

            if (context.HttpContext.User != null
                && context.HttpContext.User.Identity.IsAuthenticated)
            {
                if (context.HttpContext.User.HasClaim(ClaimsPrincipalFactory.EmailVerified, true.ToString()) &&
                    context.HttpContext.User.HasClaim(ClaimsPrincipalFactory.PhoneVerfiied, true.ToString()))
                {
                    var orgId = context.HttpContext.User.Claims.Where(claim => claim.Type == ClaimsPrincipalFactory.CurrentOrgId).FirstOrDefault();
                    if ((((string)context.RouteData.Values["controller"]).ToLower() != "verifyidentity") &&
                            (orgId == null || String.IsNullOrEmpty(orgId.Value) || orgId.Value == ClaimsPrincipalFactory.None) &&
                            !((ctrlDescriptor.ControllerName == "Organization" && ctrlDescriptor.ActionName == "Create")))
                    {
                        context.Result = new RedirectToActionResult("Create", "Organization", null);
                    }
                }
                else if (((string)context.RouteData.Values["controller"]).ToLower() != "NotAuthorized")
                {
                    context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { Controller = "NotAuthorized", View="SystemAdmin" }));
                }
            }
        }
    }
}
