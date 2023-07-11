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

using System;
using IoT.SDK.Device;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Transport;
using NLog;

namespace IoT.Device.Demo
{
    public class MessageSample : MessagePublishListener, RawDeviceMessageListener, DeviceCustomMessageListener
    {
        public void FunMessageSample(string serverUri, int port, string deviceId, string deviceSecret)
        {
            // Creates a device.
            IoTDevice device = new IoTDevice(serverUri, port, deviceId, deviceSecret);

            if (device.Init() != 0)
            {
                return;
            }

            device.GetClient().Report(new PubMessage(new DeviceMessage("hello11")));
            device.GetClient().deviceCustomMessageListener = this;
            device.GetClient().messagePublishListener = this;
            device.GetClient().rawDeviceMessageListener = this;

            // Reports a message with a custom topic. You must configure the custom topic on the platform and set the topic prefix to $oc/devices/{device_id}/user/. Use Postman to simulate the scenario in which an application uses the custom topic to deliver a command.
            string suf_topic = "wpy";
            device.GetClient().SubscribeTopic(suf_topic);

            device.GetClient().Report(new PubMessage(CommonTopic.PRE_TOPIC + suf_topic, "hello raw message "));
        }

        public void OnMessagePublished(RawMessage message)
        {
            Console.WriteLine("pubSucessMessage:" + message.Payload);
            Console.WriteLine();
        }

        public void OnMessageUnPublished(RawMessage message)
        {
            Console.WriteLine("pubFailMessage:" + message.Payload);
            Console.WriteLine();
        }

        public void OnCustomMessageCommand(string message)
        {
            Console.WriteLine("onCustomMessageCommand , message = " + message);
        }

        public void OnRawDeviceMessage(RawDeviceMessage message)
        {
            DeviceMessage deviceMessage = message.ToDeviceMessage();
            if (deviceMessage == null)
            {
                string s = message.ToUtf8String();
                Console.WriteLine($"receive device message: {s}");
            }
            else
            {
                Console.WriteLine($"receive device message in system format:");
                Console.WriteLine($"    content: {deviceMessage.content}");
                Console.WriteLine($"    deviceId: {deviceMessage.deviceId}");
                Console.WriteLine($"    name: {deviceMessage.name}");
                Console.WriteLine($"    id: {deviceMessage.id}");
            }
            Console.WriteLine("");
        }
    }
}
