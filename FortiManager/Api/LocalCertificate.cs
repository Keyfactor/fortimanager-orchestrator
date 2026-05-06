using System.Text.Json.Serialization;

namespace Keyfactor.Extensions.Orchestrators.FortiManager.Api
{
    public class LocalCertificate
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("certificate")]
        public string Certificate { get; set; }

        [JsonPropertyName("_certinfo")]
        public CertInfo CertInfo { get; set; }
    }

    public class CertInfo
    {
        [JsonPropertyName("is_ca")]
        public bool IsCa { get; set; }
    }
}
