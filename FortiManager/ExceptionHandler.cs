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

using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Keyfactor.Extensions.Orchestrator.FortiManager
{
    internal static class ExceptionHandler
    {
        internal static string FlattenException(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            Exception current = ex;
            while (current != null)
            {
                if (sb.Length > 0)
                    sb.Append(" --> ");
                sb.Append(current.Message);
                current = current.InnerException;
            }
            return sb.ToString();
        }

        internal static string GetDeviceFromProperties(string propertiesJson, ILogger logger)
        {
            logger.MethodEntry(LogLevel.Debug);

            if (string.IsNullOrEmpty(propertiesJson))
                throw new ApplicationException("Store properties are missing — 'Device' is required.");

            Dictionary<string, string> properties = JsonSerializer.Deserialize<Dictionary<string, string>>(propertiesJson)
                ?? throw new ApplicationException("Store properties could not be parsed.");

            if (!properties.TryGetValue("Device", out string device) || string.IsNullOrEmpty(device))
                throw new ApplicationException("Store property 'Device' is required but was not provided.");

            logger.LogDebug($"ExceptionHandler.GetDeviceFromProperties: device = '{device}', end");
            logger.MethodExit(LogLevel.Debug);

            return device;
        }
    }
}
