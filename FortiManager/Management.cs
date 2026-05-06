using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

using Keyfactor.Extensions.Orchestrators.FortiManager.Api;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;

using Microsoft.Extensions.Logging;

using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Keyfactor.Extensions.Orchestrators.FortiManager
{
    public class Management : IManagementJobExtension
    {
        public string ExtensionName => "";

        public JobResult ProcessJob(ManagementJobConfiguration config)
        {
            ILogger logger = LogHandler.GetClassLogger(this.GetType());
            logger.LogDebug("Management.ProcessJob: begin");

            string host = config.CertificateStoreDetails.ClientMachine;
            string adom = config.CertificateStoreDetails.StorePath;
            string device = GetDeviceFromProperties(config.CertificateStoreDetails.Properties, logger);
            string username = config.ServerUsername;
            string password = config.ServerPassword;

            FortiManagerClient client = new FortiManagerClient(logger);
            string session = string.Empty;

            try
            {
                session = client.Login(host, username, password);

                switch (config.OperationType)
                {
                    case CertStoreOperationType.Add:
                        PerformAdd(config, client, host, session, device, adom, logger);
                        break;

                    case CertStoreOperationType.Remove:
                        PerformRemove(config, client, host, session, device, adom, logger);
                        break;

                    default:
                        return new JobResult
                        {
                            Result = OrchestratorJobStatusJobResult.Failure,
                            JobHistoryId = config.JobHistoryId,
                            FailureMessage = $"Unsupported operation type: {config.OperationType}"
                        };
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Management.ProcessJob: error during {config.OperationType} for alias '{config.JobCertificate?.Alias}'");
                return new JobResult
                {
                    Result = OrchestratorJobStatusJobResult.Failure,
                    JobHistoryId = config.JobHistoryId,
                    FailureMessage = $"{config.OperationType} failed for alias '{config.JobCertificate?.Alias}' on device '{device}' (FortiManager: '{host}', ADOM: '{adom}'): {ExceptionHandler.FlattenException(ex)}"
                };
            }
            finally
            {
                if (!string.IsNullOrEmpty(session))
                    client.Logout(host, session);

                logger.LogDebug("Management.ProcessJob: end");
            }

            return new JobResult
            {
                Result = OrchestratorJobStatusJobResult.Success,
                JobHistoryId = config.JobHistoryId
            };
        }

        private static void PerformAdd(ManagementJobConfiguration config, FortiManagerClient client,
            string host, string session, string device, string adom, ILogger logger)
        {
            string alias = config.JobCertificate.Alias;
            logger.LogDebug($"Management.PerformAdd: alias='{alias}', overwrite={config.Overwrite}");

            byte[] pfxBytes = Convert.FromBase64String(config.JobCertificate.Contents);
            string pfxPassword = config.JobCertificate.PrivateKeyPassword ?? string.Empty;

            ExtractPemParts(pfxBytes, pfxPassword, out string certPem, out string keyPem);

            if (config.Overwrite)
            {
                List<LocalCertificate> existing = client.GetCertificates(host, session, device, adom);
                bool aliasExists = existing.Exists(c =>
                    string.Equals(c.Name, alias, StringComparison.OrdinalIgnoreCase));

                if (aliasExists)
                {
                    logger.LogDebug($"Management.PerformAdd: alias '{alias}' exists, updating");
                    client.UpdateCertificate(host, session, device, adom, alias, certPem, keyPem);
                    return;
                }
            }

            logger.LogDebug($"Management.PerformAdd: adding new certificate '{alias}'");
            client.AddCertificate(host, session, device, adom, alias, certPem, keyPem);
        }

        private static void PerformRemove(ManagementJobConfiguration config, FortiManagerClient client,
            string host, string session, string device, string adom, ILogger logger)
        {
            string alias = config.JobCertificate.Alias;
            logger.LogDebug($"Management.PerformRemove: alias='{alias}'");

            client.DeleteCertificate(host, session, device, adom, alias);
        }

        private static void ExtractPemParts(byte[] pfxBytes, string pfxPassword,
            out string certPem, out string keyPem)
        {
            Pkcs12StoreBuilder builder = new Pkcs12StoreBuilder();
            Pkcs12Store store = builder.Build();

            using (MemoryStream ms = new MemoryStream(pfxBytes))
            {
                store.Load(ms, pfxPassword.ToCharArray());
            }

            string alias = null;
            foreach (string entryAlias in store.Aliases)
            {
                if (store.IsKeyEntry(entryAlias))
                {
                    alias = entryAlias;
                    break;
                }
            }

            if (alias == null)
                throw new ApplicationException("No key entry found in the provided PFX.");

            X509CertificateEntry certEntry = store.GetCertificate(alias);
            AsymmetricKeyEntry keyEntry = store.GetKey(alias);

            if (certEntry == null)
                throw new ApplicationException("No certificate found in the provided PFX.");
            if (keyEntry == null)
                throw new ApplicationException("No private key found in the provided PFX.");

            using (StringWriter certWriter = new StringWriter())
            {
                PemWriter pemWriter = new PemWriter(certWriter);
                pemWriter.WriteObject(certEntry.Certificate);
                pemWriter.Writer.Flush();
                certPem = certWriter.ToString();
            }

            using (StringWriter keyWriter = new StringWriter())
            {
                PemWriter pemWriter = new PemWriter(keyWriter);
                pemWriter.WriteObject(keyEntry.Key);
                pemWriter.Writer.Flush();
                keyPem = keyWriter.ToString();
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

            logger.LogDebug($"Management.GetDeviceFromProperties: device = '{device}'");
            return device;
        }

    }
}
