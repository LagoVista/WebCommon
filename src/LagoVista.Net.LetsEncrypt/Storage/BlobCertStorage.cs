using LagoVista.Net.LetsEncrypt.Interfaces;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using System.IO;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Logging;
using LagoVista.Net.LetsEncrypt.Models;
using System;

namespace LagoVista.Net.LetsEncrypt.Storage
{
    public class BlobCertStorage : ICertStorage
    {
        IAcmeSettings _settings;

        public BlobCertStorage(IAcmeSettings settings)
        {
            _settings = settings;
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
            try
            {
                if (_settings.Diagnostics) Console.WriteLine($"[CERT.STORAGE] Requested cert for {domainName}");
                using (var ms = new MemoryStream())
                {
                    var container = await GetContainerAsync();
                    var blob = container.GetBlockBlobReference(domainName);
                    if (!await blob.ExistsAsync())
                    {
                        if (_settings.Diagnostics) Console.WriteLine($"[CERT.STORAGE] Cert not found on server for {domainName}");
                        return null;
                    }

                    if (_settings.Diagnostics) Console.WriteLine($"[CERT.STORAGE] Cert found on server for {domainName}");
                    await blob.DownloadToStreamAsync(ms);
                    if (_settings.Diagnostics) Console.WriteLine($"[CERT.STORAGE] Cert downloaded and returned byte array for {domainName}");
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[CERT.STORAGE] Error getting bytes for certificate {domainName} from blob storage {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
                return null;
            }
        }

        public async Task<string> GetResponseAsync(string challenge)
        {
            try
            {
                if (_settings.Diagnostics) Console.WriteLine($"[CERT.STORAGE] Attempt to find response for challenge {challenge}");

                var table = await GetCloudTableAsync();

                var getOperation = TableOperation.Retrieve<AcmeChallengeResponse>(AcmeChallengeResponse.PARTITIONKEY, challenge);

                var retrievedResult = await table.ExecuteAsync(getOperation);
                if (retrievedResult.Result == null)
                {
                    if (_settings.Diagnostics) Console.WriteLine($"[CERT.STORAGE] Did not find response for challenge {challenge}");
                    return null;
                }
                else
                {
                    var response = (retrievedResult.Result as AcmeChallengeResponse).Response;
                    if (_settings.Diagnostics) Console.WriteLine($"[CERT.STORAGE] Found and returning response for challenge {challenge} - {response}");
                    return response;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[CERT.STORAGE] Could not get response for challenge {challenge} to table storage {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }

        public async Task SetChallengeAndResponseAsync(string challenge, string response)
        {
            try
            {
                if (_settings.Diagnostics) Console.WriteLine($"[CERT.STORAGE] Request to store response: {response} for challenge: {challenge}");

                var model = new AcmeChallengeResponse(challenge);
                model.Response = response;

                var table = await GetCloudTableAsync();
                var operation = TableOperation.Insert(model);
                await table.ExecuteAsync(operation);

                if (_settings.Diagnostics) Console.WriteLine($"[CERT.STORAGE] Successfully stored response: {response} for challenge: {challenge}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[CERT.STORAGE] Could not store challenge/response to table storage: " + ex.Message);
                Console.ResetColor();
            }
        }

        public async Task StoreCertAsync(string domainName, byte[] bytes)
        {
            try
            {
                if (_settings.Diagnostics) Console.WriteLine($"[CERT.STORAGE] Request to store [{bytes.Length}] bytes for domain [{domainName}] to blob storage.");
                using (var ms = new MemoryStream(bytes))
                {
                    var container = await GetContainerAsync();
                    var blob = container.GetBlockBlobReference(domainName);
                    await blob.DeleteIfExistsAsync();
                    await blob.UploadFromStreamAsync(ms);
                }
                if (_settings.Diagnostics) Console.WriteLine($"[CERT.STORAGE] Stored [{bytes.Length}] bytes for domain [{domainName}] to blob storage.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[CERT.STORAGE] Could not store cert for {domainName} to Blob Storage: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
