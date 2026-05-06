using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Keyfactor.Extensions.Orchestrators.FortiManager.Api
{
    public class RpcResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("session")]
        public string Session { get; set; }

        [JsonPropertyName("result")]
        public List<RpcResult> Result { get; set; }
    }

    public class RpcResult
    {
        [JsonPropertyName("status")]
        public RpcStatus Status { get; set; }

        [JsonPropertyName("data")]
        public System.Text.Json.JsonElement Data { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class RpcStatus
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
