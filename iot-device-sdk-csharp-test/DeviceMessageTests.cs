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
using System.Text;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Transport;
using Moq;
using Xunit;

namespace IoT.SDK.Device.DeviceMessageTests
{
    public class Tests
    {
        private const string DEVICE_ID = "deviceId";

        private class TestData
        {
            public byte[] payload;
            public bool isSystemFormat;
            public bool isSubDeviceMessage;

            public TestData(byte[] payload, bool isSystemFormat, bool isSubDeviceMessage)
            {
                this.payload = payload;
                this.isSystemFormat = isSystemFormat;
                this.isSubDeviceMessage = isSubDeviceMessage;
            }

            public TestData(string payload, bool isSystemFormat, bool isSubDeviceMessage)
            {
                this.payload = Encoding.UTF8.GetBytes(payload);
                this.isSystemFormat = isSystemFormat;
                this.isSubDeviceMessage = isSubDeviceMessage;
            }
        }

        private static Mock<IoTDevice> CreateMockDevice()
        {
            string serverUri = "iot-mqtts.cn-north-4.myhuaweicloud.com";
            int port = 8883;
            string secret = "dummy";
            return new Mock<IoTDevice>(serverUri, port, DEVICE_ID, secret) { CallBase = true };
        }

        private static void SendMsg2Device(Mock<IoTDevice> device, byte[] payload)
        {
            RawMessage msg = new RawMessage("dummyMessageId")
            {
                Topic = $"$oc/devices/{DEVICE_ID}/sys/messages/down",
                BinPayload = payload
            };
            device.Object.GetClient().OnMessageReceived(msg);
        }

        private void MessageHandlerGeneralTest(Mock<IoTDevice> device, TestData data)
        {
            Console.Out.WriteLine("test {0}", Encoding.UTF8.GetString(data.payload));

            Mock<DeviceMessageListener> deviceMessageListenerMock = new Mock<DeviceMessageListener>();
            Mock<RawDeviceMessageListener> rawDeviceMessageListenerMock = new Mock<RawDeviceMessageListener>();

            device.Object.GetClient().deviceMessageListener = deviceMessageListenerMock.Object;
            device.Object.GetClient().rawDeviceMessageListener = rawDeviceMessageListenerMock.Object;

            SendMsg2Device(device, data.payload);

            rawDeviceMessageListenerMock.Verify(
                l => l.OnRawDeviceMessage(It.IsAny<RawDeviceMessage>()),
                Times.Once());

            int callTimes = (data.isSystemFormat && !data.isSubDeviceMessage) ? 1 : 0;
            deviceMessageListenerMock.Verify(
                l => l.OnDeviceMessage(It.IsAny<DeviceMessage>()),
                Times.Exactly(callTimes));
        }

        [Fact]
        private void Test_not_set_callback()
        {
            //make sure it doesn't crash when both listener are null
            Mock<IoTDevice> device = CreateMockDevice();
            SendMsg2Device(device, Encoding.UTF8.GetBytes("test data"));
            Assert.Null(device.Object.GetClient().deviceMessageListener);
            Assert.Null(device.Object.GetClient().rawDeviceMessageListener);
        }

        [Fact]
        public void Test_message_in_system_format()
        {
            TestData[] testData =
            {
                new TestData("{\"name\":\"1\",\"id\":\"2\",\"content\":\"3\",\"object_device_id\":null}", true, false),
                new TestData("{\"name\": null,\"id\":\"2\",\"content\":\"3\",\"object_device_id\":\"1\"}", true, true),
                new TestData("{\"name\":\"1\",\"id\":null,\"content\":\"3\",\"object_device_id\":\"1\"}", true, true),
                new TestData("{\"name\":\"1\",\"id\":\"2\",\"content\":\"3\"}", true, false),
                new TestData("{\"content\":\"3\", \"object_device_id\": \"" + DEVICE_ID + "\"}", true, false)
            };

            Mock<IoTDevice> device = CreateMockDevice();
            foreach (var d in testData)
            {
                MessageHandlerGeneralTest(device, d);
            }
        }

        [Fact]
        public void Test_message_in_non_system_format()
        {
            TestData[] testData =
            {
                new TestData("{\"name1\":\"1\",\"id\":\"2\",\"content\":\"3\",\"object_device_id\":null}", false,
                    false),
                new TestData("{\"content\":\"3\",\"object_device_id\":\"1\",\"object_device_id22\":\"1\"}", false,
                    true),
                new TestData("ddf", false, true),
                new TestData("{\"name\":[1],\"id\":null,\"content\":\"3\",\"object_device_id\":\"1\"}", false, false)
            };

            Mock<IoTDevice> device = CreateMockDevice();
            foreach (var d in testData)
            {
                MessageHandlerGeneralTest(device, d);
            }
        }

        [Fact]
        public void Test_message_in_binary_format()
        {
            TestData[] testData =
            {
                new TestData(new byte[] { 0x1, 0x2, 0x3, 0x4, 0x5 }, false, false),
                new TestData(new byte[] { 107, 114, 91, 29 }, false, false)
            };

            Mock<IoTDevice> device = CreateMockDevice();
            foreach (var d in testData)
            {
                MessageHandlerGeneralTest(device, d);
            }
        }

        [Fact]
        public void Test_raw_message_method()
        {
            String testData = "{\"name\":\"1\",\"id\":\"2\",\"content\":\"3\",\"object_device_id\":\"" + DEVICE_ID +
                              "\"}";
            byte[] payload = Encoding.UTF8.GetBytes(testData);

            Mock<IoTDevice> device = CreateMockDevice();
            Mock<DeviceMessageListener> deviceMessageListenerMock = new Mock<DeviceMessageListener>();
            Mock<RawDeviceMessageListener> rawDeviceMessageListenerMock = new Mock<RawDeviceMessageListener>();

            device.Object.GetClient().deviceMessageListener = deviceMessageListenerMock.Object;
            device.Object.GetClient().rawDeviceMessageListener = rawDeviceMessageListenerMock.Object;

            SendMsg2Device(device, payload);

            // Run the test
            rawDeviceMessageListenerMock.Verify(
                l => l.OnRawDeviceMessage(It.Is<RawDeviceMessage>(
                    m => m.payload == payload
                )),
                Times.Once());

            deviceMessageListenerMock.Verify(
                l => l.OnDeviceMessage(It.Is<DeviceMessage>(
                    m => m.name == "1" && m.id == "2" && m.content == "3" && m.deviceId == DEVICE_ID
                )),
                Times.Once());
        }
    }
}