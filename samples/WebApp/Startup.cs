// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 145070e23bfe9853ebdab8d88c7449ff53cf1c48dbce30e02c59cb30331f331b
// IndexVersion: 0
// --- END CODE INDEX META ---
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LagoVisata.Net.LetsEncrypt.Sample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
