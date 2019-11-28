using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using LagoVista.Net.LetsEncrypt.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.Core;

namespace LagoVista.Net.LetsEncrypt.AcmeServices.Middleware
{
    public class AcmeResponseMiddleware
    {
        static readonly PathString AcmeResponsePath = new PathString("/.well-known/acme-challenge");

        readonly RequestDelegate _next;
        readonly ILogger<AcmeResponseMiddleware> _logger;
        readonly ICertStorage _storage;
        readonly IAcmeSettings _settings;
        readonly IInstanceLogger _instanceLogger;

        public AcmeResponseMiddleware(RequestDelegate next, ICertStorage storage, IAcmeSettings settings, IInstanceLogger instanceLogger, ILogger<AcmeResponseMiddleware> logger)
        {
            _next = next;
            _storage = storage;
            _logger = logger;
            _settings = settings;
            _instanceLogger = instanceLogger;
        }

        public async Task Invoke(HttpContext context)
        {
            var requestPath = context.Request.PathBase + context.Request.Path;

            _instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "AcmeResponseMiddleware_Invoke", $"Received request", requestPath.Value.ToKVP("requestPath"));

            if (requestPath.StartsWithSegments(AcmeResponsePath, out PathString requestPathId))
            {
                var challenge = requestPathId.Value.TrimStart('/');
                _instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "AcmeResponseMiddleware_Invoke", $"Received request", challenge.ToKVP("challenge"), requestPath.Value.ToKVP("requestPath"));

                var response = await _storage.GetResponseAsync(challenge);

                if (!string.IsNullOrEmpty(response))
                {
                    this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "AcmeResponseMiddleware_Invoke", "Found challenge and sent response", response.ToKVP("response"), challenge.ToKVP("challenge"));
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync(response);
                }
                else
                {
                    this._instanceLogger.AddError("AcmeResponseMiddleware_Invoke", "Could not find challenge", challenge.ToKVP("challenge"));
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