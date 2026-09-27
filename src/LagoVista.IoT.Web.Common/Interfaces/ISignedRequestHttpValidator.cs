using LagoVista.Core.Security;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace LagoVista.Web.Common.Security
{
    public interface ISignedRequestHttpValidator
    {
        SignedRequestValidationResult ValidateRuntimeInstanceV1(HttpRequest request, string key1, string key2);
        SignedRequestValidationResult ValidateRuntimeInstanceHttpV1(HttpRequest request, string key1, string key2);
        RuntimeSignedRequestSessionValidationResult TryValidateRuntimeSession(HttpRequest request);
        void IssueRuntimeSession(HttpRequest request, HttpResponse response, string hostId, TimeSpan ttl);
        Task<SignedRequestValidationResult> ValidateServiceHttpV1Async(HttpRequest request, CancellationToken cancellationToken = default);
    }
}
