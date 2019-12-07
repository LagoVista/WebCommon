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