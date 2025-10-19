// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 68b31c6b0cb43442f94e683314fd4b39131e62c4bee1c87d8fef65da0d30c084
// IndexVersion: 0
// --- END CODE INDEX META ---
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using LagoVista.IoT.Logging.Loggers;

namespace LagoVista.Net.LetsEncrypt.AcmeServices.Interfaces
{
    public interface ICertificateManager
    {
        Task<X509Certificate2> GetCertificate(IInstanceLogger instanceLogger, string domainNames);
    }
}