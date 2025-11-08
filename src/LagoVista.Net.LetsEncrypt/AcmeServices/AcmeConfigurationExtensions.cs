// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 80ed515f1fba5461c01c9a2b17b8989bb0718f6122b1761a6a9a4a738a02efe8
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Net.LetsEncrypt.AcmeServices;
using LagoVista.Net.LetsEncrypt.AcmeServices.Interfaces;
using LagoVista.Net.LetsEncrypt.Interfaces;
using LagoVista.Net.LetsEncrypt.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    public static class AcmeConfigurationExtensions
    {
        public static void AddAcmeCertificateManager(this IServiceCollection services, IAcmeSettings settings)
        {

            services.AddSingleton(settings);
            if (settings.StorageLocation == StorageLocation.BlobStorage)
            {
                services.AddSingleton<ICertStorage, BlobCertStorage>();
            }
            else
            {
                services.AddSingleton<ICertStorage, LocalCertStorage>();
            }

            services.AddSingleton<ICertificateManager, AcmeCertificateManager>();
        }
    }
}