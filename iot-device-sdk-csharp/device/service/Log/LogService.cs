/*
 * Copyright (c) 2020-2020 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Linq;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Log.Requests;
using IoT.SDK.Device.Service;
using IoT.SDK.Device.Utils;
using NLog;
using NLog.Targets;

namespace IoT.SDK.Device.Log
{
    public class LogService : AbstractService
    {
        const string NLOG_TARGET_NAME = "IoTDA";
        public const string ServiceId = "$log";


        private DeviceLogParas deviceLogConfig;

        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        [Target(NLOG_TARGET_NAME)]
        public sealed class NLogIoTDATarget : TargetWithLayout
        {
            public LogService LogServiceInstance { get; set; }


            protected override void Write(LogEventInfo logEvent)
            {
                // ignore publish success or fail message of itself
                if (!(LogServiceInstance is { EnableNLogTarget: true }) || !LogServiceInstance.CanLog()) return;
                var logMessage = Layout.Render(logEvent);
                LogServiceInstance?.ReportLog(DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                    LogType.DeviceMessage,
                    logMessage);
            }
        }

        public abstract class LogType
        {
            public const string DeviceStatus = "DEVICE_STATUS";
            public const string DeviceProperty = "DEVICE_PROPERTY";
            public const string DeviceMessage = "DEVICE_MESSAGE";
            public const string DeviceCommand = "DEVICE_COMMAND";
        }

        public bool EnableNLogTarget { get; set; } = true;


        public override string GetServiceId()
        {
            return ServiceId;
        }

        public LogService()
        {
            deviceLogConfig = new DeviceLogParas
            {
                SwitchFlag = "OFF",
                EndTime = "19970101000000"
            };

            if (!(LogManager.Configuration.AllTargets.SingleOrDefault(x =>
                    x.Name == NLOG_TARGET_NAME) is NLogIoTDATarget logger)) return;
            logger.LogServiceInstance = this;
            LOG.Debug("find log target named {}", NLOG_TARGET_NAME);
        }


        public override void OnEvent(DeviceEvent deviceEvent)
        {
            if (deviceEvent.eventType != "log_config") return;
            this.deviceLogConfig = JsonUtil.ConvertDicToObject<DeviceLogParas>(deviceEvent.paras);
            LOG.Info("deviceLogConfig: {}", deviceLogConfig);
        }

        /// <summary>
        /// Report log to platform
        /// </summary>
        /// <param name="content">Indicates the log content</param>
        public void ReportLog(string content)
        {
            ReportLog(DateTimeOffset.Now.ToUnixTimeSeconds().ToString(), LogType.DeviceStatus, content);
        }

        /// <summary>
        /// Report log to platform
        /// </summary>
        /// <param name="type"></param>
        /// <param name="content">Indicates the log content</param>
        /// <param name="timestamp"></param>
        public void ReportLog(string timestamp, string type, string content)
        {
            if (!CanLog())
            {
                return;
            }

            var paras = new Dictionary<string, object>
            {
                { "timestamp", timestamp },
                { "type", type },
                { "content", content }
            };

            var deviceEvent = new DeviceEvent
            {
                paras = paras,
                eventType = "log_report",
                serviceId = ServiceId,
                eventTime = IotUtil.GetTimeStamp()
            };

            iotDevice.GetClient().ReportEvent(deviceEvent);
        }

        public bool CanLog()
        {
            var time = deviceLogConfig.EndTime;
            if (time != null)
            {
                time = time.Replace("T", "");
                time = time.Replace("Z", "");
            }

            DateTime now = DateTime.UtcNow;
            var formattedNow = now.ToString("yyyyMMddHHmmss");
            return deviceLogConfig.IsSwitchOn &&
                   (time == null || string.Compare(formattedNow, time, StringComparison.Ordinal) < 0);
        }
    }
}