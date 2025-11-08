// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 9425129a2913412f239bb794276ff3d808423c247c82226b120422311c5206fe
// IndexVersion: 2
// --- END CODE INDEX META ---
using Azure;
using Azure.Data.Tables;
using System;

namespace LagoVista.Net.LetsEncrypt.Models
{
    public class AcmeChallengeResponse : ITableEntity
    {
        public AcmeChallengeResponse()
        {

        }

        /* TODO: If we ever get to a point where we have so many records we need a partition key, we will probably have a new algoritm in place...very, very low usage, maybe once a week */
        public const string PARTITIONKEY = "CHALLENGE";

        public AcmeChallengeResponse(string challenge)
        {
            RowKey = challenge;
            PartitionKey = AcmeChallengeResponse.PARTITIONKEY;
        }

        public string Response { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
