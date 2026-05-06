using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

namespace Keyfactor.Extensions.Orchestrators.FortiManager.Api
{
    public class FortiManagerClient
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public FortiManagerClient(ILogger logger)
        {
            _logger = logger;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _httpClient = new HttpClient(handler);
        }

        public string Login(string host, string user, string password)
        {
            _logger.LogDebug($"FortiManagerClient.Login: connecting to {host}");

            object requestBody = new
            {
                method = "exec",
                @params = new[]
                {
                    new
                    {
                        url = "/sys/login/user",
                        data = new { user, passwd = password }
                    }
                },
                id = 1
            };

            RpcResponse response = PostRpc(host, requestBody);
            ThrowIfError(response, "/sys/login/user");

            string session = response.Session;

            if (string.IsNullOrEmpty(session))
                throw new ApplicationException($"FortiManager login to {host} succeeded but returned no session token.");

            _logger.LogDebug("FortiManagerClient.Login: session obtained");
            return session;
        }

        public void Logout(string host, string session)
        {
            _logger.LogDebug($"FortiManagerClient.Logout: closing session on {host}");

            try
            {
                object requestBody = new
                {
                    method = "exec",
                    @params = new[]
                    {
                        new { url = "/sys/logout" }
                    },
                    session,
                    id = 99
                };

                PostRpc(host, requestBody);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FortiManagerClient.Logout: non-fatal error closing session");
            }
        }

        public List<LocalCertificate> GetCertificates(string host, string session, string device, string adom)
        {
            string url = $"/pm/config/device/{device}/vdom/{adom}/vpn/certificate/local";
            _logger.LogDebug($"FortiManagerClient.GetCertificates: {url}");

            object requestBody = new
            {
                method = "get",
                @params = new[]
                {
                    new { url }
                },
                session,
                id = 2
            };

            RpcResponse response = PostRpc(host, requestBody);

            if (response.Result != null && response.Result.Count > 0 &&
                response.Result[0].Status?.Code == -6)
            {
                _logger.LogDebug("FortiManagerClient.GetCertificates: no certificates found (object not found), returning empty list");
                return new List<LocalCertificate>();
            }

            ThrowIfError(response, url);

            if (response.Result == null || response.Result.Count == 0)
                return new List<LocalCertificate>();

            RpcResult result = response.Result[0];

            if (result.Data.ValueKind == System.Text.Json.JsonValueKind.Null ||
                result.Data.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                return new List<LocalCertificate>();

            List<LocalCertificate> certs = JsonSerializer.Deserialize<List<LocalCertificate>>(
                result.Data.GetRawText(), JsonOptions) ?? new List<LocalCertificate>();

            _logger.LogDebug($"FortiManagerClient.GetCertificates: retrieved {certs.Count} certificate(s)");
            return certs;
        }

        public void AddCertificate(string host, string session, string device, string adom,
            string name, string certPem, string keyPem)
        {
            string url = $"/pm/config/device/{device}/vdom/{adom}/vpn/certificate/local";
            _logger.LogDebug($"FortiManagerClient.AddCertificate: adding '{name}' via {url}");

            object requestBody = new
            {
                method = "add",
                @params = new[]
                {
                    new
                    {
                        url,
                        data = new
                        {
                            name,
                            certificate = certPem,
                            @private_key = keyPem
                        }
                    }
                },
                session,
                id = 3
            };

            RpcResponse response = PostRpc(host, requestBody);
            ThrowIfError(response, url);

            _logger.LogDebug($"FortiManagerClient.AddCertificate: '{name}' added successfully");
        }

        public void UpdateCertificate(string host, string session, string device, string adom,
            string name, string certPem, string keyPem)
        {
            string url = $"/pm/config/device/{device}/vdom/{adom}/vpn/certificate/local/{name}";
            _logger.LogDebug($"FortiManagerClient.UpdateCertificate: updating '{name}' via {url}");

            object requestBody = new
            {
                method = "update",
                @params = new[]
                {
                    new
                    {
                        url,
                        data = new
                        {
                            certificate = certPem,
                            @private_key = keyPem
                        }
                    }
                },
                session,
                id = 4
            };

            RpcResponse response = PostRpc(host, requestBody);
            ThrowIfError(response, url);

            _logger.LogDebug($"FortiManagerClient.UpdateCertificate: '{name}' updated successfully");
        }

        public void DeleteCertificate(string host, string session, string device, string adom, string name)
        {
            string url = $"/pm/config/device/{device}/vdom/{adom}/vpn/certificate/local/{name}";
            _logger.LogDebug($"FortiManagerClient.DeleteCertificate: deleting '{name}' via {url}");

            object requestBody = new
            {
                method = "delete",
                @params = new[]
                {
                    new { url }
                },
                session,
                id = 5
            };

            RpcResponse response = PostRpc(host, requestBody);
            ThrowIfError(response, url);

            _logger.LogDebug($"FortiManagerClient.DeleteCertificate: '{name}' deleted successfully");
        }

        private RpcResponse PostRpc(string host, object requestBody)
        {
            string endpoint = $"https://{host}/jsonrpc";
            string json = JsonSerializer.Serialize(requestBody, JsonOptions);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            HttpResponseMessage httpResponse = _httpClient.Send(request);
            httpResponse.EnsureSuccessStatusCode();

            string responseBody = httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            RpcResponse rpcResponse = JsonSerializer.Deserialize<RpcResponse>(responseBody, JsonOptions);

            if (rpcResponse == null)
                throw new ApplicationException($"FortiManager returned an empty or unparseable response from {endpoint}");

            return rpcResponse;
        }

        private static void ThrowIfError(RpcResponse response, string url)
        {
            if (response.Result == null || response.Result.Count == 0)
                throw new ApplicationException($"FortiManager returned no result for url '{url}'");

            RpcStatus status = response.Result[0].Status;

            if (status == null)
                throw new ApplicationException($"FortiManager returned a result with no status for url '{url}'");

            if (status.Code != 0)
                throw new ApplicationException(
                    $"FortiManager returned error code {status.Code} for url '{url}': {status.Message}");
        }
    }
}
