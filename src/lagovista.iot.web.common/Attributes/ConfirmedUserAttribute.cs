// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: bf8326e5094cf825965545e72f8957427035b8ab9cf3b9323a11624812efb581
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.Core;
using LagoVista.Core.Validation;
using LagoVista.UserAdmin;
using LagoVista.UserAdmin.Models.Users;
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

                var loginType = context.HttpContext.User.Claims.SingleOrDefault(clm => clm.Type == ClaimsFactory.Logintype);

                if (loginType?.Value == nameof(DeviceOwnerUser))
                {
                    context.HttpContext.Response.StatusCode = 401;
                    context.HttpContext.Response.Headers.Clear();
                    var result = new InvokeResult();
                    result.Errors.Add(new ErrorMessage("Inavlid Authorization Type - API not available to device user."));
                    context.Result = new JsonResult(result);
                }
                else if (context.HttpContext.User.HasClaim(ClaimsFactory.EmailVerified, true.ToString()))
                {
                    
                    var orgId = context.HttpContext.User.Claims.Where(claim => claim.Type == ClaimsFactory.CurrentOrgId).FirstOrDefault();
                    if (orgId == null || orgId.Value == "-" || String.IsNullOrEmpty(orgId.Value) || orgId.Value == Guid.Empty.ToId())
                    {
                        if (context.HttpContext.Request.Headers.Accept.Select(fld => fld == "application/json").Any())
                        {
                            context.Result = new JsonResult(new InvokeResult()
                            {
                                RedirectURL = CommonLinks.ConfirmEmail,
                            });
                        }
                        else
                        {
                            context.Result = new RedirectResult(CommonLinks.ConfirmEmail);
                        }
                    }
                    //  The else clause here is the one that means no error, probalby could use a bit of refactoring to make that more obvious.
                }
                else
                {
                    if(context.HttpContext.Request.Headers.Accept.Select(fld=>fld == "application/json").Any())
                    {
                        context.Result = new JsonResult(new InvokeResult()
                        {
                            RedirectURL = CommonLinks.ConfirmEmail,
                        });
                    }
                    else
                    {
                        context.Result = new RedirectResult(CommonLinks.ConfirmEmail);
                    }
                }
            }
        }
    }
}
