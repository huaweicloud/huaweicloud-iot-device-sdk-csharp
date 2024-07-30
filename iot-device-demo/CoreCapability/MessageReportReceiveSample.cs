/*
 * Copyright (c) 2023-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Net.Mime;
using System.Text;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Transport;
using MQTTnet.Packets;
using Newtonsoft.Json;
using NLog;

namespace IoT.Device.Demo
{
    public class MessageReportReceiveSample : DeviceSample, RawDeviceMessageListener, ConnectListener
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        // topic: "$oc/devices/{device-id}/user/ai"
        private static readonly CustomMessageTopic OC_CUSTOM_TOPIC =
            new CustomMessageTopic("ai", true);

        // topic: "/ai/test/device/to/cloud"
        private static readonly CustomMessageTopic NO_OC_CUSTOM_TOPIC =
            new CustomMessageTopic("/ai/test/device/to/cloud", false);

        private string fullNoOcCustomTopic;

        protected override void BeforeInitDevice()
        {
            Device.GetClient().rawDeviceMessageListener = this;
        }

        protected override void RunDemo()
        {
            RunCustomTopicDemo();
            RunSystemTopicDemo();
        }

        private void RunCustomTopicDemo()
        {
            // You must configure the custom topic on the platform and set the topic prefix to $oc/devices/{device_id}/user/.
            // Use Postman to simulate the scenario in which an application uses the custom topic to deliver a command.
            Device.GetClient().SubscribeTopic(NO_OC_CUSTOM_TOPIC);
            fullNoOcCustomTopic = Device.GetClient().SubscribeTopic(OC_CUSTOM_TOPIC);

            // Reports a message with $oc custom topic.
            Device.GetClient()
                .ReportRawDeviceMessage(new RawDeviceMessage("hello $oc custom topic"), OC_CUSTOM_TOPIC);

            // Reports a message with non-$oc custom topic.
            Device.GetClient()
                .ReportRawDeviceMessage(new RawDeviceMessage("hello non-$oc custom topic"), NO_OC_CUSTOM_TOPIC);
        }

        private void RunSystemTopicDemo()
        {
            // system topic
            var testMqttV5Data = new MqttV5Data
            {
                ContentType = MediaTypeNames.Text.Plain,
                ResponseTopic = fullNoOcCustomTopic,
                CorrelationData = Encoding.UTF8.GetBytes("CorrelationData"),
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("name1", "value1"),
                    new MqttUserProperty("name2", "value2")
                }
            };
            // {"content":"hello word","object_device_id":null,"name":null,"id":null}
            Device.GetClient().ReportDeviceMessage(new DeviceMessage("hello word", testMqttV5Data));

            // report binary data
            Device.GetClient()
                .ReportRawDeviceMessage(new RawDeviceMessage(new byte[] { 0x1, 0x1, 0x2, 0x3 }, testMqttV5Data));
        }

        public void OnRawDeviceMessage(RawDeviceMessage message)
        {
            var deviceMessage = message.ToDeviceMessage();
            if (deviceMessage == null)
            {
                LOG.Info("receive device message:{}",
                    message.ToUtf8String()); // or message.payload to get raw bytes
            }
            else
            {
                LOG.Info("receive device message in system format:");
                LOG.Info("    content:{}", deviceMessage.content);
                LOG.Info("    deviceId:{}", deviceMessage.deviceId);
                LOG.Info("    name:{}", deviceMessage.name);
                LOG.Info("    id:{}", deviceMessage.id);
            }

            LOG.Info("mqtt v5:{}", JsonConvert.SerializeObject(message.MqttV5Data));
        }

        public void OnCustomRawDeviceMessage(string topic, bool topicStartsWithOc, RawDeviceMessage rawDeviceMessage)
        {
            LOG.Info("custom message received, topic:{}, topic starts with oc:{}, data:{}",
                topic, topicStartsWithOc, rawDeviceMessage.payload);
        }

        public void ConnectionLost()
        {
        }

        public void ConnectComplete()
        {
            Device.GetClient().SubscribeTopic(NO_OC_CUSTOM_TOPIC);
            fullNoOcCustomTopic = Device.GetClient().SubscribeTopic(OC_CUSTOM_TOPIC);
        }

        public void ConnectFail()
        {
        }
    }
}