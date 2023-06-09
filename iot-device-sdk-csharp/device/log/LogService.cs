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

using System.Collections.Generic;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Log.Requests;
using IoT.SDK.Device.Service;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.SDK.Device.Log
{
    public class LogService : AbstractService
    {
        public static readonly string IOT_ERROR_LOG = "iot_error_log";

        public static readonly string MQTT_CONNECTION_SUCCESS = "MQTT_CONNECTION_SUCCESS";

        public static readonly string MQTT_CONNECTION_FAILURE = "MQTT_CONNECTION_FAILURE";

        public static readonly string MQTT_CONNECTION_LOST = "MQTT_CONNECTION_LOST";

        private static readonly string SWITCH_ON = "ON";

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override void OnEvent(DeviceEvent deviceEvent)
        {
            if (deviceEvent.eventType == "log_config")
            {
                LogMessage logMessage = JsonUtil.ConvertDicToObject<LogMessage>(deviceEvent.paras);
                Log.Info("logMessage: " + logMessage);

                if (SWITCH_ON == logMessage.switchFlag)
                {
                    ReportLog(GetLogContent());
                }
            }
        }

        /// <summary>
        /// Report log to platform
        /// </summary>
        /// <param name="content">Indicates the log content</param>
        public void ReportLog(string content)
        {
            Dictionary<string, object> paras = new Dictionary<string, object>();
            paras.Add("type", "DEVICE_STATUS");
            paras.Add("content", content);

            DeviceEvent deviceEvent = new DeviceEvent();
            deviceEvent.paras = paras;
            deviceEvent.eventType = "log_report";
            deviceEvent.serviceId = "$log";
            deviceEvent.eventTime = IotUtil.GetTimeStamp();

            iotDevice.GetClient().ReportEvent(deviceEvent);
        }

        public string GetLogContent()
        {
            return IotUtil.ReadJsonFile(CommonFilePath.LOG_PATH);
        }
    }
}
