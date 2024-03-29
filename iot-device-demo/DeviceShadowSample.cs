﻿/*
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

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using IoT.SDK.Device;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.Device.Demo
{
    /// <summary>
    /// Obtains device shadow data.
    /// </summary>
    public class DeviceShadowSample : DeviceShadowListener
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private IoTDevice device;

        public void FunDeviceShadowSample(string serverUri, int port, string deviceId)
        {
            // Creates a device.
            string deviceCertPath = IotUtil.GetRootDirectory() + @"\certificate\deviceCert.pfx";
            if (!File.Exists(deviceCertPath))
            {
                Log.Error("Place the device certificate in the root directory.");

                return;
            }

            X509Certificate2 deviceCert = new X509Certificate2(deviceCertPath, "123456");

            // Creates a device with X509Certificate2.
            device = new IoTDevice(serverUri, port, deviceId, deviceCert);

            if (device.Init() != 0)
            {
                return;
            }

            device.GetClient().deviceShadowListener = this;
            
            string guid = Guid.NewGuid().ToString();

            Console.WriteLine(guid);

            string topic = CommonTopic.TOPIC_SYS_SHADOW_GET + "=" + guid;

            device.GetClient().Report(new PubMessage(topic, string.Empty));
        }

        public void OnShadowCommand(string requestId, string message)
        {
            Console.WriteLine(requestId);
            Console.WriteLine(message);
        }
    }
}
