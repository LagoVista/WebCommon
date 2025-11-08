// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 7f7cb1cb06df42f5bf8608e4a51788ff622eaa1fd4541149eca3437bea2c64e6
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Net.LetsEncrypt.Interfaces;
using System;

namespace LagoVista.Net.LetsEncrypt.Models
{
    public class AcmeSettings : IAcmeSettings
    {
        public string EmailAddress { get; set; }
        public string PfxPassword { get; set; }

        public Uri AcmeUri
        {
            //get{return Development ? new Uri("https://acme-staging.api.letsencrypt.org/directory") : new Uri("https://acme-v01.api.letsencrypt.org/directory");}
            get { return Development ? new Uri("https://acme-staging-v02.api.letsencrypt.org/directory") : new Uri("https://acme-v02.api.letsencrypt.org/directory"); }
        }

        public bool Diagnostics { get; set; } = false;
        public bool Development { get; set; } = false;

        public string StorageAccountName { get; set; }
        public string StorageKey { get; set; }

        public TimeSpan RenewalPeriod { get; set; }

        public StorageLocation StorageLocation { get; set; }

        public string StorageContainerName { get; set; }

        public string StoragePath { get; set; }

        public AcmeSettings()
        {
            RenewalPeriod = TimeSpan.FromDays(14);
        }
    }
}
