using Microsoft.AspNetCore.Http;
using System;

namespace LagoVista.Web.Common.Security
{
    public sealed class RuntimeSignedRequestSessionValidationResult
    {
        public bool Successful { get; set; }
        public string InstanceId { get; set; }
        public string OrganizationId { get; set; }
        public string UserId { get; set; }
        public string HostId { get; set; }
        public DateTime ExpiresUtc { get; set; }
    }

    public interface IRuntimeSignedRequestSessionService
    {
        RuntimeSignedRequestSessionValidationResult TryValidate(HttpRequest request);
        void Issue(HttpRequest request, HttpResponse response, string hostId, TimeSpan ttl);
    }
}
