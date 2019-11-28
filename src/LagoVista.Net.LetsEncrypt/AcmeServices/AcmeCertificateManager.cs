using System;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using System.Linq;
using Certes.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using LagoVista.Net.LetsEncrypt.AcmeServices.Interfaces;
using LagoVista.Net.LetsEncrypt.Models;
using LagoVista.Net.LetsEncrypt.Interfaces;
using Certes.Acme.Resource;
using System.Net.Http;

namespace LagoVista.Net.LetsEncrypt.AcmeServices
{
    public class AcmeCertificateManager : ICertificateManager
    {
        readonly IAcmeSettings _settings;
        readonly ICertStorage _storage;

        public AcmeCertificateManager(ICertStorage storage, IAcmeSettings settings)
        {
            _storage = storage;
            _settings = settings;
        }

        public async Task<X509Certificate2> GetCertificate(string domainName)
        {
            if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Certificate Requested for {domainName}");
            var pfx = await _storage.GetCertAsync(domainName + "X");
            if (pfx != null)
            {
                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Certificate found in storage for {domainName}");
                var cert = new X509Certificate2(pfx, _settings.PfxPassword);
                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Certificate has expire date of {cert.NotAfter}");
                if (cert.NotAfter - DateTime.UtcNow > _settings.RenewalPeriod)
                {
                    if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Certificate is valid, returning cert");
                    return cert;
                }
                else
                {
                    if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Certificate is will expire, will request new cert");
                }
            }
            else
            {
                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] did not find certificate in storage for: {domainName}");
            }

            if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Requesting new certificate for {domainName}");
            pfx = await RequestNewCertificateV2(domainName);
            if (pfx != null)
            {
                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Obtained certificate for {domainName}");
                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Storing certificate for {domainName}");
                await _storage.StoreCertAsync(domainName, pfx);
                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Stored certificate will create X509 and return {domainName}");
                return new X509Certificate2(pfx, _settings.PfxPassword);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ACMECERTMGR] Response from certificate is null for {domainName}, did not get certificate.");
                Console.ResetColor();
                return null;
            }
        }

        private async Task<AcmeResult<AuthorizationEntity>> PollResultAsync(AcmeClient client, Uri uri)
        {
            int attempt = 0;
            do
            {
                await Task.Delay(5000 * attempt);
                var auth = await client.GetAuthorization(uri);
                if (auth.Data.Status != EntityStatus.Pending)
                {
                    return auth;
                }
            }
            while (++attempt < 5);

            return null;
        }

        private async Task<Order> PollResultAsync(AcmeContext context, IOrderContext order, Uri uri)
        {
            int attempt = 0;
            do
            {
                await Task.Delay(5000 * attempt);
                //var auth = context.Authorization(uri);


                var authResult = await order.Resource();
                
                //var authResult = await auth.Resource();

                if (authResult.Status == OrderStatus.Ready)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Certificate is ready: {authResult.Status}!!!!!!!!!!!!");
                    Console.ResetColor();
                    return authResult;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Waiting for certification creation: {authResult.Status}");
                    Console.ResetColor();
                }
            }
            while (++attempt < 5);

            return null;
        }

        private async Task<AcmeResult<AuthorizationEntity>> GetAuthorizationAsync(AcmeClient client, string domainName)
        {
            try
            {
                var account = await client.NewRegistraton($"mailto:{_settings.EmailAddress}");
                account.Data.Agreement = account.GetTermsOfServiceUri();
                account = await client.UpdateRegistration(account);

                var auth = await client.NewAuthorization(new AuthorizationIdentifier
                {
                    Type = AuthorizationIdentifierTypes.Dns,
                    Value = domainName
                });

                var challenge = auth.Data.Challenges.Where(c => c.Type == ChallengeTypes.Http01).First();
                var response = client.ComputeKeyAuthorization(challenge);
                await _storage.SetChallengeAndResponseAsync(challenge.Token, response);
                var httpChallenge = await client.CompleteChallenge(challenge);
                return await PollResultAsync(client, httpChallenge.Location);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Calling {_settings.AcmeUri} to requesting certificate for  {domainName} - {ex.Message}");
                Console.ResetColor();
                return null;
            }
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

                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Created new certificate and returning byte array for {domainName}.");

                return buffer;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Calling {_settings.AcmeUri} to requesting certificate for  {domainName} - {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }

    }
}