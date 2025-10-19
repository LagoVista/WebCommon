// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 9c79e6a4e1bf8d46d10269353e05490628ee35df72f21d04acefa851ca994bd8
// IndexVersion: 0
// --- END CODE INDEX META ---
using Newtonsoft.Json;
using System.Collections.Generic;

namespace LagoVista.Net.LetsEncrypt.Models
{
    public class Error
    {

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }
    }

    public class ValidationRecord
    {

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("port")]
        public string Port { get; set; }

        [JsonProperty("addressesResolved")]
        public List<string> AddressesResolved { get; set; }

        [JsonProperty("addressUsed")]
        public string AddressUsed { get; set; }

        [JsonProperty("addressesTried")]
        public List<object> AddressesTried { get; set; }
    }

    public class AcmeResponseModel
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("error")]
        public Error Error { get; set; }
        [JsonProperty("uri")]
        public string Uri { get; set; }
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("keyAuthorization")]
        public string KeyAuthorization { get; set; }
        [JsonProperty("validationRecord")]
        public List<ValidationRecord> ValidationRecord { get; set; }
    }
}
