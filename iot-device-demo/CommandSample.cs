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
using System.Collections.Generic;
using IoT.SDK.Device;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Utils;

namespace IoT.Device.Demo
{
    /// <summary>
    /// Demonstrates how to use DeviceClient to process a command delivered by the platform.
    /// </summary>
    public class CommandSample : CommandListener
    {
        private IoTDevice device;

        public void FunCommandSample(string serverUri, int port, string deviceId, string deviceSecret)
        {
            // Creates a device.
            device = new IoTDevice(serverUri, port, deviceId, deviceSecret);

            if (device.Init() != 0)
            {
                return;
            }

            device.GetClient().commandListener = this;
        }

        public void OnCommand(string requestId, string serviceId, string commandName, Dictionary<string, object> paras)
        {
            Console.WriteLine("onCommand, serviceId = " + serviceId);
            Console.WriteLine("onCommand, name = " + commandName);
            Console.WriteLine("onCommand, paras =  " + JsonUtil.ConvertObjectToJsonString(paras));

            ////Processes a command.

            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("result", "success");

            // Sends a command response.
            device.GetClient().Report(new PubMessage(requestId, new CommandRsp(0, dic)));
        }
    }
}
