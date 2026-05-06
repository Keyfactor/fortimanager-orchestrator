using System;
using System.Collections.Generic;

using Keyfactor.Extensions.Orchestrators.FortiManager.Api;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;

using Microsoft.Extensions.Logging;

namespace Keyfactor.Extensions.Orchestrators.FortiManager
{
    public class Inventory : IInventoryJobExtension
    {
        public string ExtensionName => "";

        private ILogger _logger;

        public JobResult ProcessJob(InventoryJobConfiguration config, SubmitInventoryUpdate submitInventory)
        {
            _logger = LogHandler.GetClassLogger(this.GetType());
            _logger.LogDebug("Inventory.ProcessJob: begin");

            string host = config.CertificateStoreDetails.ClientMachine;
            string adom = config.CertificateStoreDetails.StorePath;
            string username = config.ServerUsername;
            string password = config.ServerPassword;
            string device = string.Empty;

            FortiManagerClient client = new FortiManagerClient(_logger);
            string session = string.Empty;

            List<CurrentInventoryItem> inventoryItems = new List<CurrentInventoryItem>();

            try
            {
                device = ExceptionHandler.GetDeviceFromProperties(config.CertificateStoreDetails.Properties, _logger);

                session = client.Login(host, username, password);

                List<LocalCertificate> certificates = client.GetCertificates(host, session, device, adom);

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
                    FailureMessage = $"Inventory failed for device '{device}' on FortiManager '{host}' (ADOM: '{adom}'): {ExceptionHandler.FlattenException(ex)}"
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
            _logger.LogDebug("Inventory.ConvertPemToBase64Der: begin");

            string stripped = pem
                .Replace("-----BEGIN CERTIFICATE-----", string.Empty)
                .Replace("-----END CERTIFICATE-----", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Trim();

            byte[] derBytes = Convert.FromBase64String(stripped);

            _logger.LogDebug("Inventory.ConvertPemToBase64Der: end");
            return Convert.ToBase64String(derBytes);
        }
    }
}
