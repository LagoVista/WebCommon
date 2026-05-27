using LagoVista.Core.Security;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.Web.Common.Security
{
    public interface ISignedRequestHttpValidator
    {
        SignedRequestValidationResult ValidateRuntimeInstanceV1(HttpRequest request, string key1, string key2);
        SignedRequestValidationResult ValidateRuntimeInstanceHttpV1(HttpRequest request, string key1, string key2);
        Task<SignedRequestValidationResult> ValidateServiceHttpV1Async(HttpRequest request, CancellationToken cancellationToken = default);
    }
}
