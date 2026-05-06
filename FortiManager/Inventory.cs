using System;
using System.Collections.Generic;
using System.Text.Json;

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

        public JobResult ProcessJob(InventoryJobConfiguration config, SubmitInventoryUpdate submitInventory)
        {
            ILogger logger = LogHandler.GetClassLogger(this.GetType());
            logger.LogDebug("Inventory.ProcessJob: begin");

            string host = config.CertificateStoreDetails.ClientMachine;
            string adom = config.CertificateStoreDetails.StorePath;
            string device = GetDeviceFromProperties(config.CertificateStoreDetails.Properties, logger);
            string username = config.ServerUsername;
            string password = config.ServerPassword;

            FortiManagerClient client = new FortiManagerClient(logger);
            string session = string.Empty;

            List<CurrentInventoryItem> inventoryItems = new List<CurrentInventoryItem>();

            try
            {
                session = client.Login(host, username, password);

                List<LocalCertificate> certificates = client.GetCertificates(host, session, device, adom);

                foreach (LocalCertificate cert in certificates)
                {
                    if (string.IsNullOrEmpty(cert.Certificate))
                    {
                        logger.LogDebug($"Inventory.ProcessJob: skipping '{cert.Name}' — no certificate data");
                        continue;
                    }

                    try
                    {
                        string base64Cert = ConvertPemToBase64Der(cert.Certificate);

                        CurrentInventoryItem inventoryItem = new CurrentInventoryItem
                        {
                            Alias = cert.Name,
                            Certificates = new string[] { base64Cert },
                            PrivateKeyEntry = !string.IsNullOrEmpty(cert.PrivateKey),
                            UseChainLevel = false,
                            ItemStatus = OrchestratorInventoryItemStatus.Unknown
                        };

                        inventoryItems.Add(inventoryItem);
                        logger.LogDebug($"Inventory.ProcessJob: added '{cert.Name}' to inventory");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, $"Inventory.ProcessJob: failed to process certificate '{cert.Name}', skipping");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Inventory.ProcessJob: fatal error during inventory");
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

                logger.LogDebug("Inventory.ProcessJob: end");
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
                logger.LogError(ex, "Inventory.ProcessJob: error submitting inventory to Keyfactor Command");
                return new JobResult
                {
                    Result = OrchestratorJobStatusJobResult.Failure,
                    JobHistoryId = config.JobHistoryId,
                    FailureMessage = $"Inventory retrieved {inventoryItems.Count} certificate(s) but failed to submit to Command: {ExceptionHandler.FlattenException(ex)}"
                };
            }
        }

        private static string GetDeviceFromProperties(string propertiesJson, ILogger logger)
        {
            if (string.IsNullOrEmpty(propertiesJson))
                throw new ApplicationException("Store properties are missing — 'Device' is required.");

            Dictionary<string, string> properties = JsonSerializer.Deserialize<Dictionary<string, string>>(propertiesJson)
                ?? throw new ApplicationException("Store properties could not be parsed.");

            if (!properties.TryGetValue("Device", out string device) || string.IsNullOrEmpty(device))
                throw new ApplicationException("Store property 'Device' is required but was not provided.");

            logger.LogDebug($"Inventory.GetDeviceFromProperties: device = '{device}'");
            return device;
        }

        private static string ConvertPemToBase64Der(string pem)
        {
            string stripped = pem
                .Replace("-----BEGIN CERTIFICATE-----", string.Empty)
                .Replace("-----END CERTIFICATE-----", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Trim();

            byte[] derBytes = Convert.FromBase64String(stripped);
            return Convert.ToBase64String(derBytes);
        }

    }
}
