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