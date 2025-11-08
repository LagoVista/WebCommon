// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 954211f420f96d421f418de1e11928a1e13ff4e90b0415e6007583749c601570
// IndexVersion: 2
// --- END CODE INDEX META ---
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
