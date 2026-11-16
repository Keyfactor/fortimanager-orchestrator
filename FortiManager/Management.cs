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
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Keyfactor.Extensions.Orchestrator.FortiManager
{
    public class Management : BaseJob, IManagementJobExtension
    {
        public string ExtensionName => "";

        private ILogger _logger;

        public JobResult ProcessJob(ManagementJobConfiguration config)
        {
            _logger = LogHandler.GetClassLogger(this.GetType());
            _logger.LogDebug("Management.ProcessJob: begin");

            string host = config.CertificateStoreDetails.ClientMachine;
            string username = config.ServerUsername;
            string password = config.ServerPassword;

            FortiManagerClient client = new FortiManagerClient(_logger);
            string session = string.Empty;

            bool hasWarning = false;

            try
            {
                SetValuesFromStorePath(config.CertificateStoreDetails.StorePath, _logger);

                dynamic properties = JsonConvert.DeserializeObject(config.CertificateStoreDetails.Properties.ToString());
                bool syncFortigateDevice = properties.SyncFortigateDevice == null || string.IsNullOrEmpty(properties.SyncFortigateDevice.Value) ? false : Convert.ToBoolean(properties.SyncFortigateDevice.Value);

                session = client.Login(host, username, password);

                switch (config.OperationType)
                {
                    case CertStoreOperationType.Add:
                        PerformAdd(config, client, host, session, fortimanagerDevice, fortigateVdom);
                        break;

                    case CertStoreOperationType.Remove:
                        PerformRemove(config, client, host, session, fortimanagerDevice, fortigateVdom);
                        break;

                    default:
                        return new JobResult
                        {
                            Result = OrchestratorJobStatusJobResult.Failure,
                            JobHistoryId = config.JobHistoryId,
                            FailureMessage = $"Unsupported operation type: {config.OperationType}"
                        };
                }

                if (syncFortigateDevice)
                {
                    try
                    {
                        client.SyncDevice(host, session, fortimanagerAdom, fortimanagerDevice, fortigateVdom);
                        System.Threading.Thread.Sleep(3000);
                    }
                    catch (Exception ex)
                    {
                        hasWarning = true;
                        string error = ExceptionHandler.FlattenException(ex);
                        _logger.LogError(ex, $"Certificate {config.JobCertificate.Alias} successfully added, updated or removed, but there may be an issue syncing Fortigate device {fortigateVdom} - {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                string error = ExceptionHandler.FlattenException(ex);
                _logger.LogError(ex, $"Management.ProcessJob: error during {config.OperationType} for alias '{config.JobCertificate?.Alias} - {error}.");
                return new JobResult
                {
                    Result = OrchestratorJobStatusJobResult.Failure,
                    JobHistoryId = config.JobHistoryId,
                    FailureMessage = $"{config.OperationType} failed for alias '{config.JobCertificate?.Alias}' on FortiManager instance {host}, FortiManager device '{fortimanagerDevice}' (FortiManager: '{host}', Fortigate VDOM: '{fortigateVdom}'): {error}"
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
                Result = hasWarning ? OrchestratorJobStatusJobResult.Warning : OrchestratorJobStatusJobResult.Success,
                FailureMessage = hasWarning ? $"Certificate {config.JobCertificate.Alias} successfully added, updated or removed, but there may have been an issue syncing to the Fortigate device {fortigateVdom}. Please check logs for details." : null,
                JobHistoryId = config.JobHistoryId 
            };
        }

        private void PerformAdd(ManagementJobConfiguration config, FortiManagerClient client,
            string host, string session, string device, string vdom)
        {
            _logger.MethodEntry(LogLevel.Debug);

            string alias = config.JobCertificate.Alias;
            _logger.LogDebug($"Management.PerformAdd: begin, alias='{alias}', overwrite={config.Overwrite}");

            byte[] pfxBytes = Convert.FromBase64String(config.JobCertificate.Contents);
            string pfxPassword = config.JobCertificate.PrivateKeyPassword ?? string.Empty;

            ExtractPemParts(pfxBytes, pfxPassword, out string certPem, out string keyPem);

            List<LocalCertificate> existing = client.GetCertificates(host, session, device, vdom);
            bool aliasExists = existing.Exists(c =>
                                string.Equals(c.Name, alias, StringComparison.OrdinalIgnoreCase));

            if (aliasExists && !config.Overwrite)
                throw new ApplicationException($"Certificate {alias} already exists.  Please try again with Overwrite selected to replace an existing certificate.");

            if (config.Overwrite)
            {
                if (aliasExists)
                {
                    _logger.LogDebug($"Management.PerformAdd: alias '{alias}' exists, updating");
                    client.UpdateCertificate(host, session, device, vdom, alias, certPem, keyPem);
                    _logger.LogDebug("Management.PerformAdd: end");
                    return;
                }
            }

            _logger.LogDebug($"Management.PerformAdd: adding new certificate '{alias}'");
            client.AddCertificate(host, session, device, vdom, alias, certPem, keyPem);
            
            _logger.MethodExit(LogLevel.Debug);
        }

        private void PerformRemove(ManagementJobConfiguration config, FortiManagerClient client,
            string host, string session, string device, string vdom)
        {
            _logger.MethodEntry(LogLevel.Debug);

            string alias = config.JobCertificate.Alias;
            _logger.LogDebug($"Management.PerformRemove: begin, alias='{alias}'");

            client.DeleteCertificate(host, session, device, vdom, alias);

            _logger.MethodExit(LogLevel.Debug);
        }

        private void ExtractPemParts(byte[] pfxBytes, string pfxPassword,
            out string certPem, out string keyPem)
        {
            _logger.MethodEntry(LogLevel.Debug);

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

            _logger.MethodExit(LogLevel.Debug);
        }
    }
}
