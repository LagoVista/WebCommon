using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using LagoVista.Net.LetsEncrypt.AcmeServices.Interfaces;
using LagoVista.Net.LetsEncrypt.Models;
using Microsoft.Extensions.Logging;
using System.Net;
using LagoVista.IoT.Logging.Loggers;
using WebApp;

namespace LagoVisata.Net.LetsEncrypt.Sample
{
    public class Program
    {
        /* 
         * Usage: start NGROK and point to port 5000 
         * ngrok http 8008 --hostname=nuviot-dev.ngrok.io
         */

        private const string URI = "nuviot-dev.ngrok.io";

        public static void Main(string[] args)
        {
            var settings = new AcmeSettings()
            {
                EmailAddress = "kevinw@software-logistics.com",
                StorageLocation = LagoVista.Net.LetsEncrypt.Interfaces.StorageLocation.FileSystem,
                //StorageLocation = LagoVista.Net.LetsEncrypt.Interfaces.StorageLocation.BlobStorage,
                StorageAccountName = "nuviotdev",
                StorageContainerName = "tempcert",
                StorageKey = "Za6PpxUbXjXic8rhK3lbcWyUQyVY2NVsgXRRD1rVj2LAjXUnji5/ooJx7u0ob9cPKTkPu/woa74DBE6IVKsLQA==",
                Development = true,
                Diagnostics = true,
                PfxPassword = "Test1234",
                StoragePath = @"X:\Certs"
            };

            var acmeHost = new WebHostBuilder()
                    .ConfigureLogging((factory) =>
                    {
                        factory.AddConsole();

                    })
                   .ConfigureServices(services => services.AddAcmeCertificateManager(settings))
                   //                   .UseUrls("http://*:8008/.well-known/acme-challenge/")
                   .UseUrls("http://*:8008")
                   .Configure(app => app.UseAcmeResponse())
                   .UseKestrel()
                   .Build();

            acmeHost.Start();

            var host = new WebHostBuilder()
                .ConfigureLogging((factory) =>
                {
                    factory.AddConsole();

                })
                .UseStartup<Startup>()
                .ConfigureServices(services => services.AddAcmeCertificateManager(settings))
                .UseKestrel(async (options) =>
                {
                    // Request a new certificate with Let's Encrypt and store it for next time
                    var certificateManager = options.ApplicationServices.GetService(typeof(ICertificateManager)) as ICertificateManager;
                    var instanceLogger = new InstanceLogger(new LogWriter(), "host", "1.0", "instance");

                    var certificate = await certificateManager.GetCertificate(instanceLogger, URI);
                    if (certificate != null)
                    {
                        options.Listen(IPAddress.Loopback, 443,
                            listenOptions =>
                            {
                                listenOptions.UseHttps(certificate);
                            });

                    }
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Build();

            host.Run();
        }
    }
}
