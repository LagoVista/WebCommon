using System;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using LagoVista.Net.LetsEncrypt.AcmeServices.Interfaces;
using LagoVista.Net.LetsEncrypt.Interfaces;
using Certes.Acme.Resource;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.Core;
using System.Net.Http;

namespace LagoVista.Net.LetsEncrypt.AcmeServices
{
    public class AcmeCertificateManager : ICertificateManager
    {
        private readonly IAcmeSettings _settings;
        private readonly ICertStorage _storage;
        static IInstanceLogger _instanceLogger;

        private const string Tag = "AcmeCertMgr";

        public AcmeCertificateManager(ICertStorage storage, IAcmeSettings settings)
        {
            _storage = storage ?? throw new NullReferenceException(nameof(storage));
            _settings = settings ?? throw new NullReferenceException(nameof(settings));
        }

        public async Task<X509Certificate2> GetCertificate(IInstanceLogger instanceLogger, string domainName)
        {
            AcmeCertificateManager._instanceLogger = instanceLogger ?? throw new ArgumentNullException(nameof(instanceLogger));
            this._storage.Init(this._settings, instanceLogger);

            AcmeCertificateManager._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, $"{Tag}_GetCertificate", $"Certificate Requested for {domainName}");
            var pfx = await _storage.GetCertAsync(domainName);
            if (pfx != null)
            {
                AcmeCertificateManager._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, $"{Tag}_GetCertificate", $"Certificate found in storage for {domainName}");
                var cert = new X509Certificate2(pfx, _settings.PfxPassword);
                AcmeCertificateManager._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, $"{Tag}_GetCertificate", $"Certificate has expire date of {cert.NotAfter}");
                if (cert.NotAfter - DateTime.UtcNow > _settings.RenewalPeriod)
                {
                    AcmeCertificateManager._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, $"{Tag}_GetCertificate", $"Certificate is valid, returning cert");
                    return cert;
                }
                else
                {
                    AcmeCertificateManager._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, $"{Tag}_GetCertificate", $"Certificate is will expire, will request new cert");
                }
            }
            else
            {
                AcmeCertificateManager._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, $"{Tag}_GetCertificate", $"Did not find certificate in storage for: {domainName}");
            }

            AcmeCertificateManager._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, $"{Tag}_GetCertificate", $"Requesting new certificate for {domainName}");
            pfx = await RequestNewCertificateV2(domainName);
            if (pfx != null)
            {
                AcmeCertificateManager._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, $"{Tag}_GetCertificate", $"Obtained certificate for {domainName}");
                AcmeCertificateManager._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, $"{Tag}_GetCertificate", $"Storing certificate for {domainName}");
                await _storage.StoreCertAsync(domainName, pfx);
                AcmeCertificateManager._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, $"{Tag}_GetCertificate", $"Stored certificate will create X509 and return {domainName}");
                return new X509Certificate2(pfx, _settings.PfxPassword);
            }
            else
            {
                AcmeCertificateManager._instanceLogger.AddError($"{Tag}_GetCertificate", $"Response from certificate is null for {domainName}, did not get certificate.");
                return null;
            }
        }

        public static IInstanceLogger GetInstanceLogger()
        {
            return _instanceLogger;
        }

        private async Task<Order> PollResultAsync(AcmeContext context, IOrderContext order, Uri uri)
        {
            int attempt = 0;
            do
            {
                await Task.Delay(5000 * attempt);

                //var json = Newtonsoft.Json.JsonConvert.SerializeObject(order);

                //var client = new AcmeHttpClient(context.DirectoryUri, new System.Net.Http.HttpClient());

                //var result = await AcmeHttpClientExtensions.Post<Order>(client, context, uri, null, true);

                //var authResult = result.Resource;

                var authResult = await order.Resource();

                if (authResult.Status == OrderStatus.Ready)
                {
                    AcmeCertificateManager._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, $"{Tag}_PollResultAsync", $"Certificate is ready: {authResult.Status}.");
                    return authResult;
                }
                else if (authResult.Status == OrderStatus.Invalid)
                {
                    var auth = authResult.Authorizations.FirstOrDefault();
                    if(auth != null)
                    {
                        var httpClient = new HttpClient();
                        var jsonStr = await httpClient.GetStringAsync(auth);
                        Console.WriteLine(jsonStr);
                    }

                    return null;
                }
                else
                {
                    AcmeCertificateManager._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, $"{Tag}_PollResultAsync", $"Waiting for certification creation: {authResult.Status}");
                }
            }
            while (++attempt < 5);

            return null;
        }

        private async Task<byte[]> RequestNewCertificateV2(string domainName)
        {
            var context = new AcmeContext(_settings.AcmeUri);
            await context.NewAccount(_settings.EmailAddress, true);

            var order = await context.NewOrder(new[] { domainName });
            var auths = await order.Authorizations();

            var authZ = auths.First();

            var httpChallenge = await authZ.Http();
            var key = httpChallenge.KeyAuthz;

            var challenge = httpChallenge.KeyAuthz.Split('.')[0];

            await _storage.SetChallengeAndResponseAsync(challenge, key);

            await httpChallenge.Validate();

            await PollResultAsync(context, order, order.Location);

            try
            {
                var privateKey = KeyFactory.NewKey(KeyAlgorithm.RS256);

                var cert = await order.Generate(new CsrInfo
                {
                    CountryName = "USA",
                    State = "FL",
                    Locality = "TAMPA",
                    Organization = "SOFTWARE LOGISTICS",
                    OrganizationUnit = "HOSTING",
                    CommonName = domainName,
                }, privateKey);

                var certPem = cert.ToPem();
                var pfxBuilder = cert.ToPfx(privateKey);
                var buffer = pfxBuilder.Build(domainName, _settings.PfxPassword);

                AcmeCertificateManager._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, $"{Tag}_RequestNewCertificateV2", $"Created new certificate and returning byte array for {domainName}.");

                return buffer;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                AcmeCertificateManager._instanceLogger.AddException($"{Tag}_RequestNewCertificateV2", ex, _settings.AcmeUri.ToString().ToKVP("acmeUri"), domainName.ToKVP("domainName"));
                Console.ResetColor();
                return null;
            }
        }


        public class AcmeHttpClientExtensions
        {
            /// <summary>
            /// Posts the data to the specified URI.
            /// </summary>
            /// <typeparam name="T">The type of expected result</typeparam>
            /// <param name="client">The client.</param>
            /// <param name="context">The context.</param>
            /// <param name="location">The URI.</param>
            /// <param name="entity">The payload.</param>
            /// <param name="ensureSuccessStatusCode">if set to <c>true</c>, throw exception if the request failed.</param>
            /// <returns>
            /// The response from ACME server.
            /// </returns>
            /// <exception cref="Exception">
            /// If the HTTP request failed and <paramref name="ensureSuccessStatusCode"/> is <c>true</c>.
            /// </exception>
            public static async Task<AcmeHttpResponse<T>> Post<T>(IAcmeHttpClient client,
                IAcmeContext context,
                Uri location,
                object entity,
                bool ensureSuccessStatusCode)
            {

                var payload = await context.Sign(entity, location);
                var response = await client.Post<T>(location, payload);
                var retryCount = context.BadNonceRetryCount;
                while (response.Error?.Status == System.Net.HttpStatusCode.BadRequest &&
                    response.Error.Type?.CompareTo("urn:ietf:params:acme:error:badNonce") == 0 &&
                    retryCount-- > 0)
                {
                    payload = await context.Sign(entity, location);
                    response = await client.Post<T>(location, payload);
                }

                if (ensureSuccessStatusCode && response.Error != null)
                {
                    throw new AcmeRequestException(
                        string.Format("{0}", location),
                        response.Error);
                }

                return response;
            }
        }
    }
}