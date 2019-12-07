using LagoVista.Net.LetsEncrypt.Interfaces;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using LagoVista.IoT.Logging.Loggers;

namespace LagoVista.Net.LetsEncrypt.Storage
{
    public class LocalCertStorage : ICertStorage
    {
        IAcmeSettings _settings;
        IInstanceLogger _instanceLogger;
        

        static Dictionary<string, string> _challanges = new Dictionary<string, string>();

        public Task<byte[]> GetCertAsync(string domainName)
        {
            if (!System.IO.Directory.Exists(_settings.StoragePath)) System.IO.Directory.CreateDirectory(_settings.StoragePath);

            var fullPath = $@"{_settings.StoragePath}\{domainName}.cer";
            if(System.IO.File.Exists(fullPath))
            {
                return Task.FromResult( System.IO.File.ReadAllBytes(fullPath));
            }

            return Task.FromResult(default(byte[]));         
        }

        public Task<string> GetResponseAsync(string challenge)
        {
            if (_challanges.ContainsKey(challenge))
            {
                return Task.FromResult(_challanges[challenge]);
            }
            else
            {
                return null;
            }
        }

        public Task SetChallengeAndResponseAsync(string challenge, string response)
        {
            _challanges.Add(challenge, response);
            return Task.FromResult(default(object));
        }

        public Task StoreCertAsync(string domainName, byte[] bytes)
        {
            var fullPath = $@"{_settings.StoragePath}\{domainName}.cer";

            if(!System.IO.Directory.Exists(_settings.StoragePath)) System.IO.Directory.CreateDirectory(_settings.StoragePath);

            System.IO.File.WriteAllBytes(fullPath, bytes);

            return Task.FromResult(default(object));
        }

        public void Init(IAcmeSettings settings, IInstanceLogger instanceLogger)
        {
            _settings = settings;
            _instanceLogger = instanceLogger;
        }
    }
}
