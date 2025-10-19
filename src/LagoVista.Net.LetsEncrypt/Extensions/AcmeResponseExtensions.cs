// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: eae6590815325f9aa2f38c7717eb7aae517409a24f63444c3c9a4741bdf19469
// IndexVersion: 0
// --- END CODE INDEX META ---
using Microsoft.AspNetCore.Builder;
using LagoVista.Net.LetsEncrypt.AcmeServices.Middleware;

namespace Microsoft.AspNetCore.Hosting
{
    public static class AcmeResponseExtensions
    {
        public static IApplicationBuilder UseAcmeResponse(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AcmeResponseMiddleware>();
        }
    }
}