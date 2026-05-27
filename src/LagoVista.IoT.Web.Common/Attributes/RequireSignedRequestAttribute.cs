using LagoVista.Core.Security;
using LagoVista.Web.Common.Security;
using Microsoft.AspNetCore.Mvc;
using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireSignedRequestAttribute : TypeFilterAttribute
{
    public RequireSignedRequestAttribute() : base(typeof(RequireSignedRequestFilter))
    {
        Arguments = new object[] { SignedRequestCanonicalProfile.ServiceHttpV1 };
    }

    public RequireSignedRequestAttribute(SignedRequestCanonicalProfile profile) : base(typeof(RequireSignedRequestFilter))
    {
        Arguments = new object[] { profile };
    }
}