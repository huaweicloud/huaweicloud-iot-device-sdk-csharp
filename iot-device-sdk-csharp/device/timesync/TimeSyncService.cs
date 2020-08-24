/*Copyright (c) <2020>, <Huawei Technologies Co., Ltd>
 * All rights reserved.
 * &Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice, this list of
 * conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list
 * of conditions and the following disclaimer in the documentation and/or other materials
 * provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used
 * to endorse or promote products derived from this software without specific prior written permission.
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 *
 * */

using System;
using System.Collections.Generic;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Service;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.SDK.Device.Timesync
{
    /// <summary>
    /// 时间同步服务，提供简单的时间同步服务
    /// </summary>
    public class TimeSyncService : AbstractService, MessagePublishListener
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public TimeSyncListener listener { get; set; }

        public void RequestTimeSync()
        {
            Dictionary<string, object> node = new Dictionary<string, object>();
            node.Add("device_send_time", IotUtil.GetTimeStamp());
            
            DeviceEvent deviceEvent = new DeviceEvent();
            deviceEvent.eventType = "time_sync_request";
            deviceEvent.paras = node;
            deviceEvent.serviceId = "$time_sync";
            deviceEvent.eventTime = IotUtil.GetEventTime();

            iotDevice.GetClient().messagePublishListener = this;
            iotDevice.GetClient().ReportEvent(deviceEvent);
        }

        public override void OnEvent(DeviceEvent deviceEvent)
        {
            if (listener == null)
            {
                return;
            }

            if (deviceEvent.eventType == "time_sync_response")
            {
                Dictionary<string, object> node = deviceEvent.paras;
                long device_send_time = Convert.ToInt64(node["device_send_time"]);
                long server_recv_time = Convert.ToInt64(node["server_recv_time"]);
                long server_send_time = Convert.ToInt64(node["server_send_time"]);

                listener.OnTimeSyncResponse(device_send_time, server_recv_time, server_send_time);
            }
        }

        public void OnMessagePublished(RawMessage message)
        {
            Log.Debug("reportEvent success: " + message.Payload);
        }

        public void OnMessageUnPublished(RawMessage message)
        {
            Log.Error("reportEvent fail: " + message.Payload);
        }
    }
}
