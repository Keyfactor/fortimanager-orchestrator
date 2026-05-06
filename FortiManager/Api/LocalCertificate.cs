using System.Text.Json.Serialization;

namespace Keyfactor.Extensions.Orchestrators.FortiManager.Api
{
    public class LocalCertificate
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("certificate")]
        public string Certificate { get; set; }

        [JsonPropertyName("private-key")]
        public string PrivateKey { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
}
