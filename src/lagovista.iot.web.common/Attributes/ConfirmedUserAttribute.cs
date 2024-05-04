using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.Core;
using LagoVista.UserAdmin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;

namespace LagoVista.IoT.Web.Common.Attributes
{
    public class ConfirmedUserAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            if (context.ActionDescriptor is ControllerActionDescriptor ctrlDescriptor &&
                (ctrlDescriptor.ControllerName == "Account" && ctrlDescriptor.ActionName == "LogOff"))
            {
                return;
            }

            if (context.HttpContext.User != null && context.HttpContext.User.Identity.IsAuthenticated)
            {
                /* These three methods can be called without having the user be verified and have an org created */
                if (context.HttpContext.Request.Path.StartsWithSegments(new PathString("/Account/Verify")) ||
                    context.HttpContext.Request.Path.StartsWithSegments(new PathString("/Account/CreateNewOrg")) ||
                    context.HttpContext.Request.Path.StartsWithSegments(new PathString("/api/org/factory")) ||
                    context.HttpContext.Request.Path.StartsWithSegments(new PathString("/api/verify")) ||
                    context.HttpContext.Request.Path.StartsWithSegments(new PathString("/api/user")) ||
                    context.HttpContext.Request.Path.StartsWithSegments(new PathString("/api/v1/logoff")) ||
                    context.HttpContext.Request.Path.StartsWithSegments(new PathString("/api/v1/auth")) ||
                    context.HttpContext.Request.Path.StartsWithSegments(new PathString("/mobile/login/oauth")) ||
                    context.HttpContext.Request.Path.StartsWithSegments(new PathString("/account/login/oauth")) ||
                    context.HttpContext.Request.Path.StartsWithSegments(new PathString("/api/user/register")) ||
                    context.HttpContext.Request.Path.StartsWithSegments(new PathString("/api/org/namespace")) ||
                    context.HttpContext.Request.Path.Value.ToLower() == "/api/org")
                {
                    return;
                }

                if (context.HttpContext.User.HasClaim(ClaimsFactory.EmailVerified, true.ToString()))
                {
                    var orgId = context.HttpContext.User.Claims.Where(claim => claim.Type == ClaimsFactory.CurrentOrgId).FirstOrDefault();
                    if (orgId == null || orgId.Value == "-" || String.IsNullOrEmpty(orgId.Value) || orgId.Value == Guid.Empty.ToId())
                    {
                        Console.WriteLine($"User Authenticated, but no org, redirecting to Create New Org  {context.HttpContext.Request.Path}");
                        context.Result = new RedirectResult(CommonLinks.CreateDefaultOrg);
                    }
                }
                else
                {
                    Console.WriteLine($"User Authenticated, but not verified, redirect to verify screen from {context.HttpContext.Request.Path}");
                    context.Result = new RedirectResult(CommonLinks.ConfirmEmail);
                }
            }
        }
    }
}
