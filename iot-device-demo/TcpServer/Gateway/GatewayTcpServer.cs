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


using System;
using System.Collections.Generic;
using DotNetty.Transport.Channels;
using System.Threading.Tasks;
using IoT.Bridge.Sample.Tcp.TcpDeviceMessageDecoding;
using IoT.Device.Demo.Core;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.Device.Demo.Gateway
{
    public class GatewayTcpServer : AbstractFeatureTestSample
    {
        private static readonly string SUB_DEVICES_PATH = IotUtil.GetRootDirectory() + @"\json\subdevices-{0}.json";

        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        private SimpleGateway gateway;

        public void Start(DemoConfig demoConfig)
        {
            var deviceConfig = demoConfig.AuthConfig;

            var persistence =
                new SubDevicesFilePersistence(string.Format(SUB_DEVICES_PATH, deviceConfig.DeviceId));

            gateway = deviceConfig.AuthUseCert
                ? new SimpleGateway(persistence, deviceConfig.ServerAddress,
                    deviceConfig.ServerPort, deviceConfig.DeviceId, DemoDeviceHelper.ReadDeviceCert(deviceConfig))
                : new SimpleGateway(persistence, deviceConfig.ServerAddress,
                    deviceConfig.ServerPort, deviceConfig.DeviceId, deviceConfig.DeviceSecret);

            if (gateway.Init() != 0)
            {
                return;
            }

            gateway.ReportDeleteSubDevice(demoConfig.GatewayConfig.PreDeleteSubDeviceIds);
            gateway.ReportAddSubDevice(demoConfig.GatewayConfig.PreAddSubDevice);
            Task.Run(async () =>
            {
                await GenericDemoTcpServer.CreateServer(demoConfig.GatewayConfig.ListenPort,
                    gateway.GatewaySessionManager,
                    new Dictionary<string, Func<IChannelHandler>>
                    {
                        { "UpLinkHandler", () => new UpLinkHandler(gateway) }
                    });
            });
        }


        internal class UpLinkHandler : SimpleChannelInboundHandler<TcpDeviceMessage>
        {
            private SimpleGateway SimpleGateway { get; }

            public UpLinkHandler(SimpleGateway device)
            {
                SimpleGateway = device;
            }


            protected override void ChannelRead0(IChannelHandlerContext ctx,
                TcpDeviceMessage msg)
            {
                var deviceInfo = SimpleGateway.GetSubDeviceByNodeId(msg.DeviceOrNodeId);
                var deviceId = deviceInfo.deviceId;
                switch (msg)
                {
                    case TcpDeviceLoginMessage message:
                    {
                        SimpleGateway.ReportSubDeviceStatus(deviceId, "ONLINE");
                        break;
                    }
                    case TcpDeviceLogoutMessage message:
                    {
                        SimpleGateway.ReportSubDeviceStatus(deviceId, "OFFLINE");
                        break;
                    }
                    case TcpDevicePropertiesReportMessage message:
                        SimpleGateway.ReportSubDeviceProperties(
                            deviceId,
                            message.Services);
                        break;
                    case TcpDeviceDeviceMessage message:
                        SimpleGateway.ReportSubDeviceMessage(
                            new DeviceMessage
                            {
                                content = message.Message,
                                deviceId = deviceId
                            }
                        );
                        break;
                    case TcpDeviceCommandResponseMessage message:
                        SimpleGateway.GetClient().RespondCommand(message.RequestId, message.Response);
                        break;
                }
            }
        }
    }
}