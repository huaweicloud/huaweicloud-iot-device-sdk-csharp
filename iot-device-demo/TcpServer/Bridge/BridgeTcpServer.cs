/*
 * Copyright (c) 2022-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using IoT.Device.Demo.Core;
using IoT.Device.Demo.Gateway;
using Iot.Device.Demo.HubDevice.Bridge;
using IoT.SDK.Bridge.Bootstrap;

namespace IoT.Device.Demo.HubDevice.Bridge
{
    public class BridgeTcpServer : AbstractFeatureTestSample
    {
        public void Start(DemoConfig demoConfig)
        {
            // 启动时初始化网桥连接

            BridgeClientConf conf = new BridgeClientConf
            {
                ServerIp = demoConfig.AuthConfig.ServerAddress,
                ServerPort = demoConfig.AuthConfig.ServerPort,
                BridgeId = demoConfig.AuthConfig.DeviceId,
                BridgeSecret = demoConfig.AuthConfig.DeviceSecret,
            };
            // 设置平台下行数据监听器
            var bridgeBootstrap = new BridgeBootstrap();
            bridgeBootstrap.InitBridge(conf);
            var bridgeClient = bridgeBootstrap.GetBridgeDevice().bridgeClient;

            if (Environment.GetEnvironmentVariable("ENV_NET_BRIDGE_TEST_ONLY") == "true")
            {
                new BridgeTest().Start(bridgeClient);
            }
            else
            {
                BridgeSessionManger bridgeSessionManger = new BridgeSessionManger();
                DownLinkListener downLinkListener = new DownLinkListener(bridgeClient, bridgeSessionManger);
                bridgeClient.bridgeCommandListener = downLinkListener; // 设置平台命令下发监听器
                bridgeClient.bridgeDeviceMessageListener = downLinkListener; // 设置平台消息下发监听器
                bridgeClient.bridgeDeviceDisConnListener = downLinkListener; // 设置平台通知网桥主动断开设备连接的监听器

                // 启动TCP服务
                Task.Run(async () =>
                {
                    await GenericDemoTcpServer.CreateServer(
                        demoConfig.BridgeConfig.ListenPort,
                        bridgeSessionManger,
                        new Dictionary<string, Func<IChannelHandler>>
                        {
                            { "BridgeUpLinkHandler", () => new BridgeUpLinkHandler(bridgeClient) }
                        });
                });
            }
        }
    }
}