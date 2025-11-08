// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: b59d64524720d406bffa33a1d2a67279489b46cb312976a3e7652a0332e57d43
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Net.LetsEncrypt.Interfaces
{
    public interface ICertStorage
    {
        void Init(IAcmeSettings settings, IInstanceLogger instanceLogger);

        Task SetChallengeAndResponseAsync(string challenge, string response);
        Task<String> GetResponseAsync(string challenge);

        Task StoreCertAsync(string domainName, byte[] bytes);
        Task<byte[]> GetCertAsync(string domainName);
    }
}
