/*
 * Copyright (c) 2023-2023 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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

using IoT.SDK.Device;
using IoT.SDK.Device.Bootstrap;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Transport;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace IoT.Device.Demo
{
    public class BootstrapSample : BootstrapMessageListener
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        BootstrapClient bootstrapClient;

        private string bootstrapUri;

        private int port;

        private string deviceId;

        private string deviceSecret;

        private IoTDevice device;

        public void FunBootstrapSample(string bootstrapUri, int port, string deviceId, string deviceSecret)
        {
            this.bootstrapUri = bootstrapUri;
            this.port = port;
            this.deviceSecret = deviceSecret;
            this.deviceId = deviceId;

            //创建引导客户端，发起引导
            bootstrapClient = new BootstrapClient(bootstrapUri, port, deviceId, deviceSecret);

            bootstrapClient.bootstrapMessageListener = this;

            bootstrapClient.Bootstrap();
        }

        public void OnBootstrapMessage(string payload)
        {
            JObject obj = JObject.Parse(payload);

            string address = obj["address"].ToString();

            Log.Info("bootstrap success:" + address);

            // 引导成功后关闭客户端
            bootstrapClient.Close();

            string serverUri = address.Split(':')[0];

            int port = int.Parse(address.Split(':')[1]);

            device = new IoTDevice(serverUri, port, deviceId, deviceSecret);
            if (device.Init() != 0)
            {
                return;
            }

            device.GetClient().ReportDeviceMessage(new DeviceMessage("hello"));

            device.GetClient().bootstrapMessageListener = this;
        }

        public void OnRetryBootstrapMessage()
        {
            //断开已有设备并重引导
            device.GetClient().Close();

            bootstrapClient = new BootstrapClient(bootstrapUri, port, deviceId, deviceSecret);

            bootstrapClient.bootstrapMessageListener = this;

            bootstrapClient.Bootstrap();
        }
    }
}
