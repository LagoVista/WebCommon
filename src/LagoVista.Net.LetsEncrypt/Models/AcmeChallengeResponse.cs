// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: b0dd78aefa00dfa9d3ecec682c1cdb0b7de3b84b6f9edf2224ee06db45a808a6
// IndexVersion: 0
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
