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
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.X509;

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
            if(_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Certifiate Requested for {domainName}");
            var pfx = await _storage.GetCertAsync(domainName);
            if (pfx != null)
            {
                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Certifiate found in storage for {domainName}");
                var cert = new X509Certificate2(pfx, _settings.PfxPassword);
                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Certifiate has expire date of {cert.NotAfter}");
                if (cert.NotAfter - DateTime.UtcNow > _settings.RenewalPeriod)
                {
                    if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Certifiate is valid, returning cert");
                    return cert;
                }
                else
                {
                    if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Certifiate is will expire, will request new cert");
                }
            }
            else
            {
                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] did not find certificate in storage for: {domainName}");
            }

            if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Requesting new certificate for {domainName}");
            pfx = await RequestNewCertificate(domainName);
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

        private async Task<AcmeResult<Authorization>> PollResultAsync(AcmeClient client, Uri uri)
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

        private async Task<AcmeResult<Authorization>> GetAuthorizationAsync(AcmeClient client, string domainName)
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

        private async Task<byte[]> RequestNewCertificate(String domainName)
        {
            using (var client = new AcmeClient(_settings.AcmeUri))
            {
                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Calling {_settings.AcmeUri} to requesting certificate for  {domainName}.");

                var result = await GetAuthorizationAsync(client, domainName);

                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Made call to {_settings.AcmeUri} for new cert for {domainName}.");

                if (result.Data.Status != EntityStatus.Valid)
                {                    
                    var acmeResponse = JsonConvert.DeserializeObject<AcmeResponseModel>(result.Json);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ACMECERTMGR] Did not succeed in call to get new cert: {acmeResponse.Status} {acmeResponse.Error.Detail}.");
                    Console.ResetColor();

                    return null;
                }

                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Success making call to {_settings.AcmeUri} for new cert for {domainName}.");
                
                var csr = new CertificationRequestBuilder();
                csr.AddName("CN", domainName);                
                var cert = await client.NewCertificate(csr);
                var buffer = cert.ToPfx().Build(domainName, _settings.PfxPassword);

                if (_settings.Diagnostics) Console.WriteLine($"[ACMECERTMGR] Created new certificate and returning byte array for {domainName}.");

                return buffer;
            }
        }
    }
}