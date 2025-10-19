// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 7814d8e7ed7b74a011bfdbd07d7382edbd965578557906f8cf39becb83b7aa68
// IndexVersion: 0
// --- END CODE INDEX META ---
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
            var instanceLogger = AcmeCertificateManager.GetInstanceLogger();
            if (instanceLogger == null)
            {
                throw new NullReferenceException("Instance Logger null on AcmeCertificationManger.");
            }

            try
            {
                context.Response.Headers.Add("Date", DateTime.UtcNow.ToJSONString());

                var requestPath = context.Request.PathBase + context.Request.Path;

                _storage.Init(_settings, instanceLogger);

                Console.WriteLine($"[AcmeResponseMiddleware__Invoke] Request Received {requestPath.Value}");

                instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "AcmeResponseMiddleware_Invoke", $"Received request", requestPath.Value.ToKVP("requestPath"));

                if (requestPath.StartsWithSegments(AcmeResponsePath, out PathString requestPathId))
                {
                    Console.WriteLine($"[AcmeResponseMiddleware__Invoke] Matching Request");

                    var challenge = requestPathId.Value.TrimStart('/');
                    instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "AcmeResponseMiddleware_Invoke", $"Received request", challenge.ToKVP("challenge"), requestPath.Value.ToKVP("requestPath"));

                    var response = await _storage.GetResponseAsync(challenge);

                    if (!string.IsNullOrEmpty(response))
                    {
                        Console.WriteLine($"[AcmeResponseMiddleware__Invoke] Found and forwarded response");

                        context.Response.ContentType = "text/plain";
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync(response);
                        await context.Response.Body.FlushAsync();

                        instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "AcmeResponseMiddleware_Invoke", "Found challenge and flushed response", response.ToKVP("response"), challenge.ToKVP("challenge"));
                    }
                    else
                    {
                        instanceLogger.AddError("AcmeResponseMiddleware_Invoke", "Could not find challenge", challenge.ToKVP("challenge"));
                        Console.WriteLine($"[AcmeResponseMiddleware__Invoke] Could not find and forward response");
                        context.Response.StatusCode = 404;
                        await context.Response.Body.FlushAsync();
                    }
                }
                else
                {
                    await _next.Invoke(context);
                }
            }
            catch (Exception ex)
            {
                instanceLogger.AddException("AcmeResponseMiddleware_Invoke", ex, context.Request.Path.ToString().ToKVP("path"));
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[AcmeResponseMiddleware__Invoke] ERROR!!!! {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}