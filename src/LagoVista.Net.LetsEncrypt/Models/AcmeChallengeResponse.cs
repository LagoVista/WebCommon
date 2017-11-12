using Microsoft.WindowsAzure.Storage.Table;


namespace LagoVista.Net.LetsEncrypt.Models
{
    public class AcmeChallengeResponse : TableEntity
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
    }
}
