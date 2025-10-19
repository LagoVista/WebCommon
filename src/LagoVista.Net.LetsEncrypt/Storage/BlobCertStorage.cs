// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 119436a6e17d17abf881b2ffd9c9f1c65d535d788afbd18c1ff01202739b3ba9
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.Net.LetsEncrypt.Interfaces;
using System.Threading.Tasks;
using System.IO;
using LagoVista.Net.LetsEncrypt.Models;
using System;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.Core;
using Azure.Data.Tables;
using Azure.Storage.Blobs;

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

        private async Task<TableClient> GetTableClientAsync()
        {
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_settings.StorageAccountName};AccountKey={_settings.StorageKey}";

            var tableName = _settings.StorageContainerName;
            var tableClient = new TableClient(connectionString, tableName);

            await tableClient.CreateIfNotExistsAsync();
                        
            return tableClient;
        }

        private async Task<BlobContainerClient> GetBlobContainerClientAsync()
        {
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_settings.StorageAccountName};AccountKey={_settings.StorageKey}";
            var blobClient = new BlobServiceClient(connectionString);
            try
            {
                var blobContainerClient = blobClient.GetBlobContainerClient(_settings.StorageContainerName);
                return blobContainerClient;
            }
            catch (Exception ex)
            {
                var container = await blobClient.CreateBlobContainerAsync(_settings.StorageContainerName);

                return container.Value;
            }
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
                    var container = await GetBlobContainerClientAsync();
                    var blobClient = container.GetBlobClient(domainName);
                    var blobContent = await blobClient.DownloadContentAsync();

                    this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "BlobCertStorage_GetCertAsync", "Cert found", domainName.ToKVP("domainName"));

                    var buffer = blobContent.Value.Content.ToArray();

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

                var table = await GetTableClientAsync();
                var entity = await table.GetEntityAsync<AcmeChallengeResponse>(AcmeChallengeResponse.PARTITIONKEY, challenge);



                if (entity.GetRawResponse().Status != 200 || entity.Value == null)
                {
                    this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Error, "BlobCertStorage_GetResponseAsync", "Could not find challenge.", challenge.ToKVP("challenge"));
                    return null;
                }
                else
                {
                    var value = entity.Value.Response;
                    this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "BlobCertStorage_GetResponseAsync", "Found Response for challenge",
                        challenge.ToKVP("challenge"), value.ToKVP("response"));
                    return value;
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

                var client = await GetTableClientAsync();

                var addResponse = await client.AddEntityAsync(model);
                if (addResponse.Status < 200 && addResponse.Status > 299)
                    throw new InvalidOperationException("Could not add challenge to table storage.");

                this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "BlobCertStorage_SetChallengeAndResponseAsync", "Wrote challenge and response.",
                    challenge.ToKVP("challenge"), response.ToKVP("response"));
            }
            catch (Exception ex)
            {
                _instanceLogger.AddException("BlobCertStorage_SetChallengeAndResponseAsync", ex, challenge.ToKVP("challenge"), response.ToKVP("response"));
                throw;
            }
        }

        public async Task StoreCertAsync(string domainName, byte[] bytes)
        {
            if (_settings == null || _instanceLogger == null) throw new InvalidOperationException("[StoreCertAsync] Not initialized.");

            try
            {
                if (bytes == null) throw new ArgumentNullException(nameof(bytes));
                if (String.IsNullOrEmpty(domainName)) throw new ArgumentNullException(nameof(domainName));

                this._instanceLogger.AddCustomEvent(Core.PlatformSupport.LogLevel.Verbose, "BlobCertStorage_StoreCertAsync", "Storing SSL Cert to storage.",
                    bytes.Length.ToString().ToKVP("byteCount"), domainName.ToKVP("domainName"));

                var container = await GetBlobContainerClientAsync();
                var blobClient = container.GetBlobClient(domainName);

                using (var stream = await blobClient.OpenWriteAsync(true))
                {
                    stream.Write(bytes, 0, bytes.Length);
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
