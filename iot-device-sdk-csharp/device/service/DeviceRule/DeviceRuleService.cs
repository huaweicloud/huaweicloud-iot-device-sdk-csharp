/*
 * Copyright (c) 2024-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this list of
 *    conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list
 *    of conditions and the following disclaimer in the documentation and/or other materials
 *    provided with the distribution.
 *
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used
 *    to endorse or promote products derived from this software without specific prior written
 *    permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Gateway.Requests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace IoT.SDK.Device.Service.DeviceRule
{
    public class DeviceRuleService : AbstractService
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        public const string ServiceId = "$device_rule";

        private DeviceRuleExecutor deviceRuleExecutor;
        private string deviceRuleStoragePath;
        public Action<string> OnRuleUpdated { get; set; }

        public DeviceRuleCommandListener DeviceRuleCommandListener { get; set; }

        public string DeviceRuleStoragePath
        {
            get => deviceRuleStoragePath;
            set
            {
                if (CheckIfInitialized())
                {
                    return;
                }

                deviceRuleStoragePath = value;
            }
        }

        private bool enableDeviceRule = false;

        public bool EnableDeviceRule
        {
            get => enableDeviceRule;
            set
            {
                if (CheckIfInitialized())
                {
                    return;
                }

                enableDeviceRule = value;
            }
        }


        public bool IsInit { get; set; } = false;

        private bool CheckIfInitialized()
        {
            if (IsInit)
            {
                LOG.Error("DeviceRuleService has already been initialized");
            }

            return IsInit;
        }

        public override string GetServiceId()
        {
            return ServiceId;
        }

        private void OnDeviceRuleCommand(Command command)
        {
            if (command.deviceId != iotDevice.deviceId)
            {
                DeviceRuleCommandListener?.OnDeviceRuleCommand(ServiceId, command);
            }
            else
            {
                iotDevice.GetClient().OnCommand(ServiceId, command);
            }
        }

        public void Init()
        {
            if (CheckIfInitialized())
            {
                return;
            }

            IsInit = true;

            if (!EnableDeviceRule) return;
            deviceRuleExecutor = new DeviceRuleExecutor
            {
                RuleUpdatedAction = SaveRuleToFile,
                CommandAction = OnDeviceRuleCommand
            };

            if (DeviceRuleStoragePath != null)
            {
                var filePath = Path.GetFullPath(DeviceRuleStoragePath);
                LOG.Info("load rule from file:{}", filePath);
                try
                {
                    using var outputFile = new StreamReader(filePath);
                    var rules = JsonConvert.DeserializeObject<List<Rule>>(outputFile.ReadToEnd());
                    deviceRuleExecutor.UpdateRules(rules);
                }
                catch (Exception e)
                {
                    LOG.Error(e, "error when loading rule from local file");
                }
            }

            LOG.Info("device rule initialized");
        }

        private void SaveRuleToFile(string r)
        {
            OnRuleUpdated?.Invoke(r);

            var filePath = DeviceRuleStoragePath;
            if (filePath == null)
            {
                return;
            }

            using (var outputFile = new StreamWriter(filePath))
            {
                outputFile.Write(r);
            }

            LOG.Info("rule saved");
        }

        public void AfterConnected()
        {
            GetShadow();
        }

        public override void OnEvent(DeviceEvent deviceEvent)
        {
            if (deviceRuleExecutor == null)
            {
                return;
            }

            if (deviceEvent.eventType != "device_rule_config_response")
            {
                LOG.Error("unknown event type:{}", deviceEvent.eventType);
            }

            var value = (JToken)deviceEvent.paras["rulesInfos"];
            var rules = value.ToObject<List<Rule>>();
            var addRules = deviceRuleExecutor.UpdateRules(rules);
            LOG.Debug("add rule:{}", addRules.Select(r => r.RuleId));

            var p = rules?.ToDictionary(r => r.RuleId, r =>
                (object)new Dictionary<string, int>
                {
                    { "version", r.RuleVersionInShadow }
                });
            iotDevice.GetClient().ReportProperties(new List<ServiceProperty>
            {
                new ServiceProperty
                {
                    serviceId = ServiceId,
                    properties = p
                }
            });
        }

        public override IotResult OnWrite(Dictionary<string, object> properties)
        {
            if (deviceRuleExecutor == null)
            {
                return IotResult.SUCCESS;
            }


            var newRuleVersion = new Dictionary<string, int>();
            foreach (var (ruleId, value) in properties)
            {
                var version = ((JObject)value).GetValue("version").Value<int>();
                newRuleVersion[ruleId] = version;
            }

            deviceRuleExecutor.DeleteOrGetLackingRules(newRuleVersion, out var deletedRuleIds, out var newRuleIds);

            if (deletedRuleIds.Count > 0)
            {
                iotDevice.GetClient().ReportProperties(new List<ServiceProperty>
                {
                    new ServiceProperty
                    {
                        serviceId = ServiceId,
                        properties = deletedRuleIds.ToDictionary(key => key, value => (object)-1)
                    }
                });
                LOG.Debug("delete rule:{}", deletedRuleIds);
            }

            if (newRuleIds.Count <= 0) return IotResult.SUCCESS;

            iotDevice.GetClient().ReportEvent(new DeviceEvent
            {
                serviceId = ServiceId,
                eventType = "device_rule_config_request",
                paras = new Dictionary<string, object>
                {
                    { "ruleIds", newRuleIds.ToArray() },
                    { "delIds", deletedRuleIds.ToArray() }
                }
            });
            LOG.Debug("pull new rule:{}", newRuleIds);

            return IotResult.SUCCESS;
        }

        public void CacheProperties(List<DeviceProperty> properties)
        {
            deviceRuleExecutor?.ReportProperties(properties);
        }
    }
}