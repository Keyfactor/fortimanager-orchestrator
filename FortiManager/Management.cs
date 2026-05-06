using System;
using System.Collections.Generic;
using System.IO;
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

        private ILogger _logger;

        public JobResult ProcessJob(ManagementJobConfiguration config)
        {
            _logger = LogHandler.GetClassLogger(this.GetType());
            _logger.LogDebug("Management.ProcessJob: begin");

            string host = config.CertificateStoreDetails.ClientMachine;
            string adom = config.CertificateStoreDetails.StorePath;
            string username = config.ServerUsername;
            string password = config.ServerPassword;
            string device = string.Empty;

            FortiManagerClient client = new FortiManagerClient(_logger);
            string session = string.Empty;

            try
            {
                device = ExceptionHandler.GetDeviceFromProperties(config.CertificateStoreDetails.Properties, _logger);

                session = client.Login(host, username, password);

                switch (config.OperationType)
                {
                    case CertStoreOperationType.Add:
                        PerformAdd(config, client, host, session, device, adom);
                        break;

                    case CertStoreOperationType.Remove:
                        PerformRemove(config, client, host, session, device, adom);
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
                _logger.LogError(ex, $"Management.ProcessJob: error during {config.OperationType} for alias '{config.JobCertificate?.Alias}'");
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

                _logger.LogDebug("Management.ProcessJob: end");
            }

            return new JobResult
            {
                Result = OrchestratorJobStatusJobResult.Success,
                JobHistoryId = config.JobHistoryId
            };
        }

        private void PerformAdd(ManagementJobConfiguration config, FortiManagerClient client,
            string host, string session, string device, string adom)
        {
            string alias = config.JobCertificate.Alias;
            _logger.LogDebug($"Management.PerformAdd: begin, alias='{alias}', overwrite={config.Overwrite}");

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
                    _logger.LogDebug($"Management.PerformAdd: alias '{alias}' exists, updating");
                    client.UpdateCertificate(host, session, device, adom, alias, certPem, keyPem);
                    _logger.LogDebug("Management.PerformAdd: end");
                    return;
                }
            }

            _logger.LogDebug($"Management.PerformAdd: adding new certificate '{alias}'");
            client.AddCertificate(host, session, device, adom, alias, certPem, keyPem);
            _logger.LogDebug("Management.PerformAdd: end");
        }

        private void PerformRemove(ManagementJobConfiguration config, FortiManagerClient client,
            string host, string session, string device, string adom)
        {
            string alias = config.JobCertificate.Alias;
            _logger.LogDebug($"Management.PerformRemove: begin, alias='{alias}'");

            client.DeleteCertificate(host, session, device, adom, alias);

            _logger.LogDebug("Management.PerformRemove: end");
        }

        private void ExtractPemParts(byte[] pfxBytes, string pfxPassword,
            out string certPem, out string keyPem)
        {
            _logger.LogDebug("Management.ExtractPemParts: begin");

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

            _logger.LogDebug("Management.ExtractPemParts: end");
        }
    }
}
