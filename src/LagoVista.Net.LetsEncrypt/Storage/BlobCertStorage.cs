using LagoVista.Net.LetsEncrypt.Interfaces;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using System.IO;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Logging;
using LagoVista.Net.LetsEncrypt.Models;
using System;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.Core;

namespace LagoVista.Net.LetsEncrypt.Storage
{
    public class BlobCertStorage : ICertStorage
    {
        IAcmeSettings _settings;
        IInstanceLogger _instanceLogger;

        public void Init(IAcmeSettings settings, IInstanceLogger instanceLogger)
        {
            _settings = settings;
            _instanceLogger = instanceLogger;
        }

        private async Task<CloudTable> GetCloudTableAsync()
        {
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_settings.StorageAccountName};AccountKey={_settings.StorageKey}";
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(_settings.StorageContainerName);
            await table.CreateIfNotExistsAsync();

            return table;
        }

        private async Task<CloudBlobContainer> GetContainerAsync()
        {
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_settings.StorageAccountName};AccountKey={_settings.StorageKey}";
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(_settings.StorageContainerName);
            await container.CreateIfNotExistsAsync();

            return container;
        }

        public async Task<byte[]> GetCertAsync(string domainName)
        {
            if (_settings == null || _instanceLogger == null) throw new InvalidOperationException("[GetCertAsync] Not initialized.");

            try
            {
                if (String.IsNullOrEmpty(domainName)) throw new ArgumentNullException(nameof(domainName));

                this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "BlobCertStorage_GetCertAsync", "Requested Cert", domainName.ToKVP("domainName"));
                using (var ms = new MemoryStream())
                {
                    var container = await GetContainerAsync();
                    var blob = container.GetBlockBlobReference(domainName);
                    if (!await blob.ExistsAsync())
                    {
                        this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Error, "BlobCertStorage_GetCertAsync", "Cert not found", domainName.ToKVP("domainName"));
                        return null;
                    }

                    this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "BlobCertStorage_GetCertAsync", "Cert found", domainName.ToKVP("domainName"));
                    await blob.DownloadToStreamAsync(ms);
                    var buffer = ms.ToArray();
                    this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "BlobCertStorage_GetCertAsync", "Cert downloaded",
                        buffer.Length.ToString().ToKVP("bufferLen"), domainName.ToKVP("domainName"));
                    return buffer;
                }
            }
            catch (Exception ex)
            {
                _instanceLogger.AddException("BlobCertStorage_GetCertAsync", ex, domainName.ToKVP("domainName"));
                return null;
            }
        }

        public async Task<string> GetResponseAsync(string challenge)
        {
            if (_settings == null || _instanceLogger == null) throw new InvalidOperationException("[GetResponseAsync] Not initialized.");

            try
            {
                if (String.IsNullOrEmpty(challenge)) throw new ArgumentNullException(nameof(challenge));

                this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "BlobCertStorage_GetCertAsync", "Attempt to get response for challenge.", challenge.ToKVP("challenge"));

                var table = await GetCloudTableAsync();

                var getOperation = TableOperation.Retrieve<AcmeChallengeResponse>(AcmeChallengeResponse.PARTITIONKEY, challenge);

                var retrievedResult = await table.ExecuteAsync(getOperation);
                if (retrievedResult.Result == null)
                {
                    this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Error, "BlobCertStorage_GetResponseAsync", "Could not find challenge.", challenge.ToKVP("challenge"));
                    return null;
                }
                else
                {
                    var response = (retrievedResult.Result as AcmeChallengeResponse).Response;
                    this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "BlobCertStorage_GetResponseAsync", "Found Response for challenge", 
                        challenge.ToKVP("challenge"), response.ToKVP("response"));
                    return response;
                }
            }
            catch (Exception ex)
            {
                _instanceLogger.AddException("BlobCertStorage_GetResponseAsync", ex, challenge.ToKVP("challenge"));
                return null;
            }
        }

        public async Task SetChallengeAndResponseAsync(string challenge, string response)
        {
            if (_settings == null || _instanceLogger == null) throw new InvalidOperationException("[SetChallengeAndResponseAsync] Not initialized.");

            try
            {
                if (String.IsNullOrEmpty(challenge)) throw new ArgumentNullException(nameof(challenge));
                if (String.IsNullOrEmpty(response)) throw new ArgumentNullException(nameof(response));

                this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "BlobCertStorage_SetChallengeAndResponseAsync", "Attempt to write challenge and response.", 
                    challenge.ToKVP("challenge"), response.ToKVP("response"));

                var model = new AcmeChallengeResponse(challenge);
                model.Response = response;

                var table = await GetCloudTableAsync();
                var operation = TableOperation.Insert(model);
                await table.ExecuteAsync(operation);

                this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "BlobCertStorage_SetChallengeAndResponseAsync", "Wrote challenge and response.", 
                    challenge.ToKVP("challenge"), response.ToKVP("response"));
            }
            catch (Exception ex)
            {
                _instanceLogger.AddException("BlobCertStorage_SetChallengeAndResponseAsync", ex, challenge.ToKVP("challenge"), response.ToKVP("response"));
            }
        }

        public async Task StoreCertAsync(string domainName, byte[] bytes)
        {
            if (_settings == null || _instanceLogger == null) throw new InvalidOperationException("[StoreCertAsync] Not initialized.");

            try
            {
                if(bytes == null) throw new ArgumentNullException(nameof(bytes));
                if (String.IsNullOrEmpty(domainName)) throw new ArgumentNullException(nameof(domainName));

                this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "BlobCertStorage_StoreCertAsync", "Storing SSL Cert to storage.",
                    bytes.Length.ToString().ToKVP("byteCount"), domainName.ToKVP("domainName"));

                using (var ms = new MemoryStream(bytes))
                {
                    var container = await GetContainerAsync();
                    var blob = container.GetBlockBlobReference(domainName);
                    await blob.DeleteIfExistsAsync();
                    await blob.UploadFromStreamAsync(ms);
                }
                this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "BlobCertStorage_StoreCertAsync", "Stored SSL Cert to storage.",
                  bytes.Length.ToString().ToKVP("byteCount"), domainName.ToKVP("domainName"));
            }
            catch (Exception ex)
            {
                _instanceLogger.AddException("BlobCertStorage_StoreCertAsync", ex, domainName.ToKVP("domainName"));
            }
        }
    }
}
