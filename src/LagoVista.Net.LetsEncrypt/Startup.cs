using LagoVista.Net.LetsEncrypt.AcmeServices;
using LagoVista.Net.LetsEncrypt.AcmeServices.Interfaces;
using LagoVista.Net.LetsEncrypt.Interfaces;
using LagoVista.Net.LetsEncrypt.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LagoVista.Net.LetsEncrypt
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services, IAcmeSettings acmeSettings)
        {
            if (acmeSettings.StorageLocation == StorageLocation.BlobStorage)
            {
                services.AddTransient<ICertStorage, BlobCertStorage>();
            }
            else
            {
                services.AddTransient<ICertStorage, LocalCertStorage>();
            }

            services.AddTransient<ICertificateManager, AcmeCertificateManager>();
        }
    }
}
