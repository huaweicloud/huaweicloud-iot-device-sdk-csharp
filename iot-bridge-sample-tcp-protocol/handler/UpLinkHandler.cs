/*
 * Copyright (c) 2022-2022 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Text;
using DotNetty.Transport.Channels;
using IoT.SDK.Device.Utils;
using IoT.Bridge.Sample.Tcp.Dto;
using IoT.Bridge.Sample.Tcp.Constant;
using IoT.Bridge.Sample.Tcp.Session;
using IoT.Bridge.Sample.Tcp.Bridge;
using IoT.SDK.Device.Client.Requests;
using NLog;

namespace IoT.Bridge.Sample.Tcp.Handler {
    class UpLinkHandler : SimpleChannelInboundHandler<string> {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override void ChannelRead(IChannelHandlerContext ctx, object msg)
        {
            Log.Info("receive msg={}", JsonUtil.ConvertObjectToJsonString(msg));
            if (!(msg is BaseMessage)) {
                return;
            }
            upLinkDataHandle(ctx, (BaseMessage)msg);
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, string msg) {}

        public override void ChannelInactive(IChannelHandlerContext ctx)
        {
            string deviceId = NettyUtils.GetDeviceId(ctx.Channel);
            if (deviceId == null) {
                return;
            }
            DeviceSession deviceSession = DeviceSessionManger.GetInstance().GetSession(deviceId);
            if (deviceSession == null) {
                return;
            }

            // 调用网桥的logout接口，通知平台设备离线
            BridgeService.GetBridgeClient().LogoutAsync(deviceId, Guid.NewGuid().ToString());
            DeviceSessionManger.GetInstance().DeleteSession(deviceId);

            ctx.CloseAsync();
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
        {
            Log.Warn("exceptionCaught. channelId={0}, cause={1}", ctx.Channel.Id, exception.StackTrace);
        }

        private void upLinkDataHandle(IChannelHandlerContext ctx, BaseMessage message)
        {
            switch (message.msgHeader.msgType) {
                // DEVICE_LOGIN代表设备上线
                case Constants.MSG_TYPE_DEVICE_LOGIN:
                    Login(ctx.Channel, message);
                    break;

                // 定时位置上报
                case Constants.MSG_TYPE_REPORT_LOCATION_INFO:
                    ReportProperties(ctx.Channel, message);
                    break;

                // 位置上报周期的响应消息
                case Constants.MSG_TYPE_FREQUENCY_LOCATION_SET:
                    ResponseCommand(message);
                    break;
                default:
                    break;
            }
        }

        private void Login(IChannel channel, BaseMessage message)
        {

            if (!(message is DeviceLoginMessage)) {
                return;
            }

            string deviceId = message.msgHeader.deviceId;
            string secret = ((DeviceLoginMessage)message).secret;
            DeviceSession deviceSession = new DeviceSession();

            int resultCode = BridgeService.GetBridgeClient().LoginSync(deviceId, secret, 5000);

            // 登录成功保存会话信息
            if (resultCode == 0) {
                deviceSession.deviceId = deviceId;
                deviceSession.channel = channel;
                DeviceSessionManger.GetInstance().CreateSession(deviceId, deviceSession);
                NettyUtils.SetDeviceId(channel, deviceId);
            }

            // 构造登录响应的消息头
            MsgHeader msgHeader = new MsgHeader();
            msgHeader.deviceId = deviceId;
            msgHeader.flowNo = message.msgHeader.flowNo;
            msgHeader.direct = Constants.DIRECT_CLOUD_RSP;
            msgHeader.msgType = Constants.MSG_TYPE_DEVICE_LOGIN;

            // 调用网桥login接口，向平台发起登录请求
            BridgeService.GetBridgeClient().LoginAsync(deviceId, secret, message.msgHeader.flowNo);
        }

        private void ReportProperties(IChannel channel, BaseMessage message)
        {
            String deviceId = message.msgHeader.deviceId;
            DeviceSession deviceSession = DeviceSessionManger.GetInstance().GetSession(deviceId);
            if (deviceSession == null) {
                Log.Warn("device={} is not login", deviceId);
                SendResponse(channel, message, 1);
                return;
            }

            ServiceProperty serviceProperty = new ServiceProperty();
            serviceProperty.serviceId = "Location";
            serviceProperty.properties = JsonUtil.ConvertJsonStringToObject<Dictionary<string, object>>(JsonUtil.ConvertObjectToJsonString(message));

            List<ServiceProperty> properties = new List<ServiceProperty>();
            properties.Add(serviceProperty);
            // 调用网桥reportProperties接口，上报设备属性数据
            BridgeService.GetBridgeClient().ReportProperties(deviceId, properties);
        }

        private void ResponseCommand(BaseMessage message)
        {
            string deviceId = message.msgHeader.deviceId;
            DeviceSession deviceSession = DeviceSessionManger.GetInstance().GetSession(deviceId);
            if (deviceSession == null) {
                Log.Warn("device={0} is not login", deviceId);
                return;
            }

            // 获取平台的requestId
            string requestId = RequestIdCache.GetInstance().RemoveRequestId(deviceId, message.msgHeader.flowNo);
            if (requestId == null) {
                Log.Warn("device={0} get requestId failed", deviceId);
                return;
            }

            if (!(message is CommonResponse)) {
                Log.Warn("device={0} invalid message", deviceId);
                return;
            }

            // 调用网桥接口返回命令响应
            CommonResponse response = (CommonResponse)message;
            BridgeService.GetBridgeClient().RespondCommand(deviceId, requestId, new CommandRsp(response.resultCode));
        }

        private void SendResponse(IChannel channel, BaseMessage message, int resultCode)
        {
            CommonResponse commonResponse = new CommonResponse();
            MsgHeader msgHeader = new MsgHeader();
            msgHeader.deviceId = message.msgHeader.deviceId;
            msgHeader.flowNo = message.msgHeader.flowNo;
            msgHeader.msgType = message.msgHeader.msgType;
            msgHeader.direct = Constants.DIRECT_CLOUD_RSP;
            commonResponse.msgHeader = msgHeader;
            commonResponse.resultCode = resultCode;

            // 给设备返回登陆的响应消息
            channel.WriteAndFlushAsync(commonResponse);
        }
    }
}
