using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using LagoVista.Net.LetsEncrypt.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.Core;
using System;

namespace LagoVista.Net.LetsEncrypt.AcmeServices.Middleware
{
    public class AcmeResponseMiddleware
    {
        static readonly PathString AcmeResponsePath = new PathString("/.well-known/acme-challenge");

        readonly RequestDelegate _next;
        readonly ICertStorage _storage;
        readonly IAcmeSettings _settings;

        public AcmeResponseMiddleware(RequestDelegate next, ICertStorage storage, IAcmeSettings settings)
        {
            _next = next ?? throw new NullReferenceException(nameof(next));
            _storage = storage ?? throw new NullReferenceException(nameof(storage));
            _settings = settings ?? throw new NullReferenceException(nameof(settings));
        }

        public async Task Invoke(HttpContext context)
        {
            var requestPath = context.Request.PathBase + context.Request.Path;

            var instanceLogger = AcmeCertificateManager.GetInstanceLogger();
            if (instanceLogger == null)
            {
                throw new NullReferenceException("Instance Logger null on AcmeCertificationManger.");
            }

            _storage.Init(_settings, instanceLogger);

            instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "AcmeResponseMiddleware_Invoke", $"Received request", requestPath.Value.ToKVP("requestPath"));

            if (requestPath.StartsWithSegments(AcmeResponsePath, out PathString requestPathId))
            {
                var challenge = requestPathId.Value.TrimStart('/');
                instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "AcmeResponseMiddleware_Invoke", $"Received request", challenge.ToKVP("challenge"), requestPath.Value.ToKVP("requestPath"));

                var response = await _storage.GetResponseAsync(challenge);

                if (!string.IsNullOrEmpty(response))
                {
                    instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "AcmeResponseMiddleware_Invoke", "Found challenge and sent response", response.ToKVP("response"), challenge.ToKVP("challenge"));

                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync(response);
                }
                else
                {
                    instanceLogger.AddError("AcmeResponseMiddleware_Invoke", "Could not find challenge", challenge.ToKVP("challenge"));

                    context.Response.StatusCode = 404;
                }
            }
            else
            {
                await _next.Invoke(context);
            }
        }
    }
}