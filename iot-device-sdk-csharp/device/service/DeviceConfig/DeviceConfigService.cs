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
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.SDK.Device.Service.DeviceConfig
{
    public class DeviceConfigService : AbstractService
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        public const string ServiceId = "$device_config";
        public DeviceConfigListener DeviceConfigListener { get; set; }

        public override string GetServiceId()
        {
            return ServiceId;
        }

        public override void OnEvent(DeviceEvent deviceEvent)
        {
            if (deviceEvent.eventType != "config_update")
            {
                LOG.Error("unknown event type:{}", deviceEvent.eventType);
                return;
            }

            DeviceConfigResponse resp;
            try
            {
                resp = DeviceConfigListener?.OnDeviceConfig(deviceEvent.paras, iotDevice.deviceId) ??
                       new DeviceConfigResponse
                       {
                           ResultCode = -1,
                           Description = "configuration wasn't handled properly"
                       };
            }
            catch (Exception e)
            {
                resp = new DeviceConfigResponse
                {
                    ResultCode = -1,
                    Description = e.GetBaseException().Message
                };
            }

            iotDevice.GetClient().ReportEvent(new DeviceEvent
            {
                serviceId = ServiceId,
                eventType = "config_update_response",
                eventTime = IotUtil.GetEventTime(),
                eventId = deviceEvent.eventId,
                paras = new Dictionary<string, object>
                {
                    { "result_code", resp.ResultCode },
                    { "description", resp.Description }
                }
            });
        }
    }
}