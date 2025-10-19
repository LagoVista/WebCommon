// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 3cd00ca39529c317478c909e05ad91dd7b0970905ce413565d28ee840ec9ed85
// IndexVersion: 0
// --- END CODE INDEX META ---
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
         * 
         * ngrok http 5001 --hostname=nuviot-dev.ngrok.io
         * 
         * If you set "Development" = false, the root of the cert you get will be invalid so 
         * an exception will be thrown in RequestNewCertificateV2
         * 
         * similar to
         * Cannot find issuer 'C=US,O=(STAGING) Internet Security Research Group,CN=(STAGING) Doctored Durian Root CA X3' for certificate 'C=US,O=(STAGING) Internet Security Research Group,CN=(STAGING) Pretend Pear X1'.
         * [Error] Response from certificate is null for nuviot-dev.ngrok.io, did not get certificate.
         * 
         * Setting "Development" = true will succeed but there is a rate limit for domains.
         * 
         * When testing it first looks to a known source to find the cert if it finds it there, 
         * it won't attempt to request one.  A good way to get around this is use file system
         * and delete the file.
         * 
         */

        private const string URI = "nuviot-dev.ngrok.io";

        public static void Main(string[] args)
        {
            var accountId = Environment.GetEnvironmentVariable("TEST_AZURESTORAGE_ACCOUNTID");
            var accountKey = Environment.GetEnvironmentVariable("TEST_AZURESTORAGE_ACCESSKEY");

            if (String.IsNullOrEmpty(accountId)) throw new ArgumentNullException("Please add TEST_AZURESTORAGE_ACCOUNTID as an environnment variable");
            if (String.IsNullOrEmpty(accountKey)) throw new ArgumentNullException("Please add TEST_AZURESTORAGE_ACCESSKEY as an environnment variable");

            var settings = new AcmeSettings()
            {
                EmailAddress = "kevinw@software-logistics.com",
                StorageLocation = LagoVista.Net.LetsEncrypt.Interfaces.StorageLocation.FileSystem,
                //StorageLocation = LagoVista.Net.LetsEncrypt.Interfaces.StorageLocation.BlobStorage,
                StorageAccountName = accountId,
                StorageContainerName = "tempcert",
                StorageKey = accountKey,
                Development = false,
                Diagnostics = true,
                PfxPassword = "Test1234",
                StoragePath = @"x:\Certs"
            };

            var acmeHost = new WebHostBuilder()
                    .ConfigureLogging((factory) =>
                    {
                        factory.AddConsole();

                    })
                   .ConfigureServices(services => services.AddAcmeCertificateManager(settings))
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
