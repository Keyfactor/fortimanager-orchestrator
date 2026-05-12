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

using Keyfactor.Extensions.Orchestrator.FortiManager.Api;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Keyfactor.Extensions.Orchestrator.FortiManager
{
    public class Inventory : IInventoryJobExtension
    {
        public IPAMSecretResolver _resolver;
        public string ExtensionName => "";

        private ILogger _logger;

        public Inventory(IPAMSecretResolver resolver)
        {
            _resolver = resolver;
        }

        public JobResult ProcessJob(InventoryJobConfiguration config, SubmitInventoryUpdate submitInventory)
        {
            _logger = LogHandler.GetClassLogger(this.GetType());
            _logger.LogDebug($"Begin {config.Capability} for job id {config.JobId}...");
            _logger.LogDebug($"Client Machine: {config.CertificateStoreDetails.ClientMachine}");
            _logger.LogDebug($"Store Path: {config.CertificateStoreDetails.StorePath}");

            string host = config.CertificateStoreDetails.ClientMachine;
            string username = PAMUtilities.ResolvePAMField(_resolver, _logger, "FortiManager UserId", config.ServerUsername);
            string password = PAMUtilities.ResolvePAMField(_resolver, _logger, "FortiManager User Password", config.ServerPassword);
            string fortimanagerDevice = string.Empty;
            string fortigateVdom = string.Empty;

            FortiManagerClient client = new FortiManagerClient(_logger);
            string session = string.Empty;

            List<CurrentInventoryItem> inventoryItems = new List<CurrentInventoryItem>();

            try
            {
                List<string> vdomDevice = config.CertificateStoreDetails.StorePath.Split('|').ToList<string>();
                if (vdomDevice.Count != 2)
                    throw new ApplicationException($"Error parsing StorePath {config.CertificateStoreDetails.StorePath}.  Value must contain 2 values concatenated with a pipe character (|).");

                fortimanagerDevice = vdomDevice[0];
                fortigateVdom = vdomDevice[1];

                session = client.Login(host, username, password);

                List<LocalCertificate> certificates = client.GetCertificates(host, session, fortimanagerDevice, fortigateVdom);

                foreach (LocalCertificate cert in certificates)
                {
                    if (string.IsNullOrEmpty(cert.Certificate))
                    {
                        _logger.LogDebug($"Inventory.ProcessJob: skipping '{cert.Name}' — no certificate data");
                        continue;
                    }

                    try
                    {
                        string base64Cert = ConvertPemToBase64Der(cert.Certificate);

                        CurrentInventoryItem inventoryItem = new CurrentInventoryItem
                        {
                            Alias = cert.Name,
                            Certificates = new string[] { base64Cert },
                            PrivateKeyEntry = !cert.CertInfo.IsCa,
                            UseChainLevel = false,
                            ItemStatus = OrchestratorInventoryItemStatus.Unknown
                        };

                        inventoryItems.Add(inventoryItem);
                        _logger.LogDebug($"Inventory.ProcessJob: added '{cert.Name}' to inventory");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Inventory.ProcessJob: failed to process certificate '{cert.Name}', skipping");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inventory.ProcessJob: fatal error during inventory");
                return new JobResult
                {
                    Result = OrchestratorJobStatusJobResult.Failure,
                    JobHistoryId = config.JobHistoryId,
                    FailureMessage = $"Inventory failed for on FortiManager instance {host}, FortiManager device '{fortimanagerDevice}' (FortiManager: '{host}', Fortigate VDOM: '{fortigateVdom}'): {ExceptionHandler.FlattenException(ex)}"
                };
            }
            finally
            {
                if (!string.IsNullOrEmpty(session))
                    client.Logout(host, session);

                _logger.LogDebug("Inventory.ProcessJob: end");
            }

            try
            {
                submitInventory.Invoke(inventoryItems);
                return new JobResult
                {
                    Result = OrchestratorJobStatusJobResult.Success,
                    JobHistoryId = config.JobHistoryId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inventory.ProcessJob: error submitting inventory to Keyfactor Command");
                return new JobResult
                {
                    Result = OrchestratorJobStatusJobResult.Failure,
                    JobHistoryId = config.JobHistoryId,
                    FailureMessage = $"Inventory retrieved {inventoryItems.Count} certificate(s) but failed to submit to Command: {ExceptionHandler.FlattenException(ex)}"
                };
            }
        }

        private string ConvertPemToBase64Der(string pem)
        {
            _logger.MethodEntry(LogLevel.Debug);

            string stripped = pem
                .Replace("-----BEGIN CERTIFICATE-----", string.Empty)
                .Replace("-----END CERTIFICATE-----", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Trim();

            byte[] derBytes = Convert.FromBase64String(stripped);

            _logger.MethodExit(LogLevel.Debug);

            return Convert.ToBase64String(derBytes);
        }
    }
}
