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
using System.Linq;

namespace Keyfactor.Extensions.Orchestrator.FortiManager
{
    public class BaseJob
    {
        public string fortimanagerAdom = string.Empty;
        public string fortimanagerDevice = string.Empty;
        public string fortigateVdom = string.Empty;

        public void SetValuesFromStorePath(string storePath, ILogger logger)
        {
            logger.MethodEntry(LogLevel.Debug);

            List<string> adomVdomDevice = storePath.Split('|').ToList<string>();

            try
            {
                if (adomVdomDevice.Count != 3)
                    throw new ApplicationException($"Error parsing StorePath {storePath}.  Value must contain 3 values (FortiManager ADOM, FortiManager device name, and FortiGate VDOM concatenated with a pipe character (|).");

                fortimanagerAdom = adomVdomDevice[0];
                fortimanagerDevice = adomVdomDevice[1];
                fortigateVdom = adomVdomDevice[2];
            }
            finally
            {
                logger.MethodExit(LogLevel.Debug);
            }
        }
    }
}
