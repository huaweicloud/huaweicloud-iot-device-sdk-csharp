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
using Xunit;
using IoT.SDK.Bridge.Clent;
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Transport;
using System.Collections.Generic;
using IoT.SDK.Bridge.Request;
using Moq;

namespace IoT.SDK.Bridge.Test {
    public class BridgeClientTest {
        private static string deviceId = "deviceId";

        private static string password = "password";

        private static string requestId = "requestId";

        // Before
        private ClientConf SetUp()
        {
            ClientConf clientConf = new ClientConf();
            clientConf.ServerUri = "iot-mqtts.cn-north-4.myhuaweicloud.com";
            clientConf.Port = 8883;
            clientConf.DeviceId = "bridge1";
            clientConf.Secret = "bridge1";
            clientConf.Mode = ClientConf.CONNECT_OF_BRIDGE_MODE;
            return clientConf;
        }

        /**
        * 用例编号:Test_client_login_async_should_return_success
        * 用例标题:网桥设备发布异步login成功
        * 用例级别:Level 1
        * 预置条件:网桥已连上平台，正确的网桥设备Id和密钥
        * 预期结果:回调函数被成功调用，并且回调了topic也为登录信息
        * 修改记录:2022/11/04 
        */
        [Fact]
        public void Test_client_login_async_should_return_success()
        {
            ClientConf clientConf = SetUp();
            string topic = string.Empty;
            var mock = new Mock<BridgeClient>(clientConf, null) { CallBase = true };
            mock.Setup(x => x.Report<PubMessage>(It.IsAny<PubMessage>())).Callback<PubMessage>((pubMsg) => {
                topic = pubMsg.Topic;
            });
            mock.Object.Connect();
            mock.Object.LoginAsync(deviceId, password, requestId);

            Assert.True(topic.Contains("sys/login") == true);
            Assert.True(topic.Contains("oc/bridges/") == true);
        }

        /**
        * 用例编号:Test_client_login_async_should_return_failure
        * 用例标题:网桥设备发布异步login失败
        * 用例级别:Level 1
        * 预置条件:网桥已连上平台，正确的网桥设备Id和密钥
        * 预期结果:回调函数被成功调用，并且回调了topic也为登录信息
        * 修改记录:2022/11/04 
        */
        [Fact]
        public void Test_client_login_async_should_return_failure()
        {
            ClientConf clientConf = SetUp();
            string topic = string.Empty;

            var mock = new Mock<BridgeClient>(clientConf, null) { CallBase = true };
            mock.Setup(x => x.Report<PubMessage>(It.IsAny<PubMessage>())).Callback<PubMessage>((pubMsg) => {
                topic = pubMsg.Topic;
                throw new Exception("Login Failed");
            });
            mock.Object.Connect();
            var ex = Assert.Throws<Exception>(() => mock.Object.LoginAsync(deviceId, password, requestId));

            Assert.Equal("Login Failed", ex.Message);
            Assert.True(topic.Contains("sys/login") == true);
            Assert.True(topic.Contains("oc/bridges/") == true);
        }

        /**
        * 用例编号:Test_client_logout_async_should_return_success
        * 用例标题:网桥设备发布异步logout成功
        * 用例级别:Level 1
        * 预置条件:网桥已连上平台，正确的网桥设备Id和密钥
        * 预期结果:回调函数被成功调用，并且回调了topic也为登录信息
        * 修改记录:2022/11/04 
        */
        [Fact]
        public void Test_client_logout_async_should_return_success()
        {
            ClientConf clientConf = SetUp();
            string topic = string.Empty;

            var mock = new Mock<BridgeClient>(clientConf, null) { CallBase = true };
            mock.Setup(x => x.Report<PubMessage>(It.IsAny<PubMessage>())).Callback<PubMessage>((pubMsg) => {
                topic = pubMsg.Topic;
            });
            mock.Object.Connect();
            mock.Object.LogoutAsync(deviceId, requestId);

            Assert.True(topic.Contains("sys/logout") == true);
            Assert.True(topic.Contains("oc/bridges/") == true);
        }

        /**
        * 用例编号:Test_client_logout_async_should_return_failure
        * 用例标题:网桥设备发布异步login失败
        * 用例级别:Level 1
        * 预置条件:网桥已连上平台，正确的网桥设备Id和密钥
        * 测试步骤:提前设置监听器，然后网桥设备登录
        * 预期结果:回调函数被成功调用，并且回调了topic也为登录信息
        * 修改记录:2022/11/04 
        */
        [Fact]
        public void Test_client_logout_async_should_return_failure()
        {
            ClientConf clientConf = SetUp();
            string topic = string.Empty;

            var mock = new Mock<BridgeClient>(clientConf, null) { CallBase = true };
            mock.Setup(x => x.Report<PubMessage>(It.IsAny<PubMessage>())).Callback<PubMessage>((pubMsg) => {
                topic = pubMsg.Topic;
            });
            mock.Object.Connect();
            mock.Object.LogoutAsync(deviceId, requestId);

            Assert.True(topic.Contains("sys/logout") == true);
            Assert.True(topic.Contains("oc/bridges/") == true);
        }

        /**
         * 用例编号:Test_client_report_properties_success
         * 用例标题:网桥设备发布属性成功
         * 用例级别:Level 1
         * 预置条件:网桥已连上平台，正确的网桥设备Id和密钥
         * 测试步骤:网桥上报设备属性
         * 预期结果:connect链接成功调用一次发布接口
         * 修改记录:2022/11/04 
         */
        [Fact]
        public void Test_client_report_properties_success()
        {
            ClientConf clientConf = SetUp();
            string topic = string.Empty;

            var mock = new Mock<BridgeClient>(clientConf, null) { CallBase = true };
            mock.Setup(x => x.Report<PubMessage>(It.IsAny<PubMessage>())).Callback<PubMessage>((pubMsg) => {
                topic = pubMsg.Topic;
            });
            mock.Object.Connect();
            mock.Object.ReportProperties(deviceId, new List<ServiceProperty>() { new ServiceProperty() });

            Assert.True(topic.Contains("sys/properties/report") == true);
            Assert.True(topic.Contains("oc/bridges/") == true);
        }

        /**
         * 用例编号:Test_client_reset_secret_success
         * 用例标题:网桥设备发布重置密钥成功
         * 用例级别:Level 1
         * 预置条件:网桥已连上平台，正确的网桥设备Id和密钥
         * 测试步骤:网桥上报重置密钥
         * 预期结果:connect链接成功调用一次发布接口
         * 修改记录:2022/11/04 
         */
        [Fact]
        public void Test_client_reset_secret_success()
        {
            ClientConf clientConf = SetUp();
            string topic = string.Empty;

            var mock = new Mock<BridgeClient>(clientConf, null) { CallBase = true };
            mock.Setup(x => x.Report<PubMessage>(It.IsAny<PubMessage>())).Callback<PubMessage>((pubMsg) => {
                topic = pubMsg.Topic;
            });
            mock.Object.Connect();
            mock.Object.ResetSecret(deviceId, requestId, new DeviceSecret("oldSecret", "newSecret"));

            Assert.True(topic.Contains("sys/reset_secret") == true);
            Assert.True(topic.Contains("oc/bridges/") == true);
        }

        /**
         * 用例编号:Test_report_device_message_success
         * 用例标题:网桥设备发布设备消息成功
         * 用例级别:Level 1
         * 预置条件:网桥已连上平台，正确的网桥设备Id和密钥
         * 测试步骤:网桥上报设备消息
         * 预期结果:connect链接成功调用一次发布接口
         * 修改记录:2022/11/04 
         */
        [Fact]
        public void Test_client_report_device_message_success()
        {
            ClientConf clientConf = SetUp();
            string topic = string.Empty;

            var mock = new Mock<BridgeClient>(clientConf, null) { CallBase = true };
            mock.Setup(x => x.Report<PubMessage>(It.IsAny<PubMessage>())).Callback<PubMessage>((pubMsg) => {
                topic = pubMsg.Topic;
            });
            mock.Object.Connect();
            mock.Object.ReportDeviceMessage(deviceId, new DeviceMessage());

            Assert.True(topic.Contains("sys/messages/up") == true);
            Assert.True(topic.Contains("oc/bridges/") == true);
        }

        /**
         * 用例编号:Test_client_respond_command_success
         * 用例标题:网桥设备发布设备命令响应结果成功
         * 用例级别:Level 1
         * 预置条件:网桥已连上平台，正确的网桥设备Id和密钥
         * 测试步骤:网桥上报设备命令响应结果
         * 预期结果:connect链接成功调用一次发布接口
         * 修改记录:2022/11/04 
         */
        [Fact]
        public void Test_client_respond_command_success()
        {
            ClientConf clientConf = SetUp();
            string topic = string.Empty;

            var mock = new Mock<BridgeClient>(clientConf, null) { CallBase = true };
            mock.Setup(x => x.Report<PubMessage>(It.IsAny<PubMessage>())).Callback<PubMessage>((pubMsg) => {
                topic = pubMsg.Topic;
            });
            mock.Object.Connect();
            mock.Object.RespondCommand(deviceId, requestId, new CommandRsp(CommandRsp.SUCCESS));

            Assert.True(topic.Contains("sys/commands/response") == true);
            Assert.True(topic.Contains("oc/bridges/") == true);
        }

        /**
         * 用例编号:Test_client_respond_prop_get_success
         * 用例标题:网桥设备发布设备属性查询结果成功
         * 用例级别:Level 1
         * 预置条件:网桥已连上平台，正确的网桥设备Id和密钥
         * 测试步骤:网桥上报设备属性查询结果
         * 预期结果:connect链接成功调用一次发布接口
         * 修改记录:2022/11/04 
         */
        [Fact]
        public void Test_client_respond_prop_get_success()
        {
            ClientConf clientConf = SetUp();
            string topic = string.Empty;

            var mock = new Mock<BridgeClient>(clientConf, null) { CallBase = true };
            mock.Setup(x => x.Report<PubMessage>(It.IsAny<PubMessage>())).Callback<PubMessage>((pubMsg) => {
                topic = pubMsg.Topic;
            });
            mock.Object.Connect();
            mock.Object.RespondPropsGet(deviceId, requestId, new List<ServiceProperty>() { new ServiceProperty() });

            Assert.True(topic.Contains("sys/properties/get/response") == true);
            Assert.True(topic.Contains("oc/bridges/") == true);
        }

        /**
         * 用例编号:Test_client_respond_prop_set_success
         * 用例标题:网桥设备发布设备属性设置结果成功
         * 用例级别:Level 1
         * 预置条件:网桥已连上平台，正确的网桥设备Id和密钥
         * 测试步骤:网桥上报设备属性设置结果
         * 预期结果:connect链接成功调用一次发布接口
         * 修改记录:2022/11/04 
         */
        [Fact]
        public void Test_client_respond_prop_set_success()
        {
            ClientConf clientConf = SetUp();
            string topic = string.Empty;

            var mock = new Mock<BridgeClient>(clientConf, null) { CallBase = true };
            mock.Setup(x => x.Report<PubMessage>(It.IsAny<PubMessage>())).Callback<PubMessage>((pubMsg) => {
                topic = pubMsg.Topic;
            });
            mock.Object.Connect();
            mock.Object.RespondPropsSet(deviceId, requestId, new IotResult(0, "success"));

            Assert.True(topic.Contains("sys/properties/set/response") == true);
            Assert.True(topic.Contains("oc/bridges/") == true);
        }

        /**
         * 用例编号:Test_client_report_event_success
         * 用例标题:网桥设备发布设备事件成功
         * 用例级别:Level 1
         * 预置条件:网桥已连上平台，正确的网桥设备Id和密钥
         * 测试步骤:网桥上报设备事件
         * 预期结果:connect链接成功调用一次发布接口
         * 修改记录:2022/11/04 
         */
        [Fact]
        public void Test_client_report_event_success()
        {
            ClientConf clientConf = SetUp();
            string topic = string.Empty;

            var mock = new Mock<BridgeClient>(clientConf, null) { CallBase = true };
            mock.Setup(x => x.Report<PubMessage>(It.IsAny<PubMessage>())).Callback<PubMessage>((pubMsg) => {
                topic = pubMsg.Topic;
            });
            mock.Object.Connect();
            mock.Object.ReportEvent(deviceId, new DeviceEvent());

            Assert.True(topic.Contains("sys/events/up") == true);
            Assert.True(topic.Contains("oc/bridges/") == true);
        }
    }
}
