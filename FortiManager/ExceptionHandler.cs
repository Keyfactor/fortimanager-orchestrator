using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace Keyfactor.Extensions.Orchestrators.FortiManager
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
            logger.LogDebug("ExceptionHandler.GetDeviceFromProperties: begin");

            if (string.IsNullOrEmpty(propertiesJson))
                throw new ApplicationException("Store properties are missing — 'Device' is required.");

            Dictionary<string, string> properties = JsonSerializer.Deserialize<Dictionary<string, string>>(propertiesJson)
                ?? throw new ApplicationException("Store properties could not be parsed.");

            if (!properties.TryGetValue("Device", out string device) || string.IsNullOrEmpty(device))
                throw new ApplicationException("Store property 'Device' is required but was not provided.");

            logger.LogDebug($"ExceptionHandler.GetDeviceFromProperties: device = '{device}', end");
            return device;
        }
    }
}
