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
using DotNetty.Transport.Channels;
using IoT.SDK.Device.Utils;
using IoT.Bridge.Sample.Tcp.TcpDeviceMessageDecoding;
using IoT.SDK.Bridge.Clent;
using IoT.SDK.Device.Client.Requests;
using NLog;

namespace Iot.Device.Demo.HubDevice.Bridge
{
    class BridgeUpLinkHandler : SimpleChannelInboundHandler<TcpDeviceMessage>
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        private readonly BridgeClient bridgeClient;


        public BridgeUpLinkHandler(BridgeClient bridgeClient)
        {
            this.bridgeClient = bridgeClient;
        }


        protected override void ChannelRead0(IChannelHandlerContext ctx, TcpDeviceMessage msg)
        {
            LOG.Info("receive msg={}", JsonUtil.ConvertObjectToJsonString(msg));
            UpLinkDataHandle(ctx, msg);
        }

        public void Logout(TcpDeviceLogoutMessage message)
        {
            // 调用网桥的logout接口，通知平台设备离线
            bridgeClient.LogoutAsync(message.DeviceOrNodeId, Guid.NewGuid().ToString());
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
        {
            LOG.Warn(exception, "exceptionCaught. channelId={0}", ctx.Channel.Id);
        }

        private void UpLinkDataHandle(IChannelHandlerContext ctx, TcpDeviceMessage msg)
        {
            var channel = ctx.Channel;
            switch (msg)
            {
                case TcpDeviceLoginMessage message:
                {
                    Login(message);
                    break;
                }
                case TcpDeviceLogoutMessage message:
                {
                    Logout(message);
                    break;
                }
                case TcpDevicePropertiesReportMessage message:
                    ReportProperties(channel, message);
                    break;
                case TcpDeviceDeviceMessage message:
                    ReportDeviceMessage(
                        new DeviceMessage
                        {
                            content = message.Message,
                            deviceId = message.DeviceOrNodeId
                        }
                    );
                    break;
                case TcpDeviceCommandResponseMessage message:
                    ResponseCommand(message);
                    break;
            }
        }

        private void Login(TcpDeviceLoginMessage message)
        {
            string deviceId = message.DeviceOrNodeId;
            string secret = message.Secret;
            bridgeClient.LoginSync(deviceId, secret, 5000);
        }

        private void ReportProperties(IChannel channel, TcpDevicePropertiesReportMessage message)
        {
            string deviceId = message.DeviceOrNodeId;

            // 调用网桥reportProperties接口，上报设备属性数据
            bridgeClient.ReportProperties(deviceId, message.Services);
        }

        private void ResponseCommand(TcpDeviceCommandResponseMessage message)
        {
            var deviceId = message.DeviceOrNodeId;
            // 调用网桥接口返回命令响应
            bridgeClient.RespondCommand(deviceId, message.RequestId, message.Response);
        }

        private void ReportDeviceMessage(DeviceMessage message)
        {
            bridgeClient.ReportDeviceMessage(message.deviceId, message);
        }
    }
}