//Copyright 2026 Keyfactor
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Keyfactor.Extensions.Orchestrator.FortiManager.Api
{
    public class RpcResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("session")]
        public JsonElement SessionRaw { get; set; }

        [JsonPropertyName("result")]
        public List<RpcResult> Result { get; set; }

        [JsonIgnore]
        public string? Session =>
            SessionRaw.ValueKind switch
            {
                JsonValueKind.String => SessionRaw.GetString(),
                JsonValueKind.Number => SessionRaw.GetInt32().ToString(),
                _ => null
            };
    }

    public class RpcResult
    {
        [JsonPropertyName("status")]
        public RpcStatus Status { get; set; }

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public System.Text.Json.JsonElement? Data { get; set; }

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
