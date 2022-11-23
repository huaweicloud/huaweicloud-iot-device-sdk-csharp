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
using IoT.SDK.Bridge.Listener;
using IoT.SDK.Bridge.Clent;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Bridge.Request;
using IoT.Bridge.Sample.Tcp.Session;
using IoT.Bridge.Sample.Tcp.Dto;
using IoT.Bridge.Sample.Tcp.Constant;
using NLog;

namespace IoT.Bridge.Sample.Tcp.Handler {
    class DownLinkHandler : BridgeDeviceMessageListener, BridgeCommandListener, BridgeDeviceDisConnListener {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private BridgeClient bridgeClient;

        public DownLinkHandler(BridgeClient bridgeClient) { this.bridgeClient = bridgeClient; }

        public void OnDeviceMessage(string deviceId, DeviceMessage deviceMessage) {}

        public void OnCommand(string deviceId, string requestId, BridgeCommand bridgeCommand)
        {
            Log.Info("onCommand deviceId={0}, requestId={1}, bridgeCommand={2}", deviceId, requestId, bridgeCommand);
            DeviceSession session = DeviceSessionManger.GetInstance().GetSession(deviceId);
            if (session == null) {
                Log.Warn("device={0} session is null", deviceId);
                return;
            }

            // 设置位置上报的周期
            if (bridgeCommand.command.commandName == "FREQUENCY_LOCATION_SET") {
                processLocationSetCommand(session, requestId, bridgeCommand);
            }
            bridgeClient.RespondCommand(deviceId, requestId, new CommandRsp(CommandRsp.SUCCESS));
        }

        private void processLocationSetCommand(DeviceSession session, string requestId, BridgeCommand bridgeCommand)
        {
            int flowNo = session.GetAndUpdateSeqId();

            // 构造消息头
            MsgHeader msgHeader = new MsgHeader();
            msgHeader.deviceId = session.deviceId;
            msgHeader.flowNo = flowNo.ToString();
            msgHeader.direct = Constants.DIRECT_CLOUD_REQ;
            msgHeader.msgType = bridgeCommand.command.commandName;

            // 根据参数内容构造消息体
            Dictionary<string, Object> paras = bridgeCommand.command.paras;
            DeviceLocationFrequencySet locationFrequencySet = new DeviceLocationFrequencySet();
            locationFrequencySet.period = Convert.ToInt32(paras["period"]);
            locationFrequencySet.msgHeader = msgHeader;

            // 发下消息到设备
            session.channel.WriteAndFlushAsync(locationFrequencySet);

            // 记录平台requestId和设备流水号的关联关系，用于关联命令的响应
            Session.RequestIdCache.GetInstance().SetRequestId(session.deviceId, flowNo.ToString(), requestId);
        }

        public void OnDisConnect(string deviceId)
        {
            // 关闭session
            DeviceSession session = DeviceSessionManger.GetInstance().GetSession(deviceId);
            if (session != null && session.channel != null) {
                session.channel.CloseAsync();
            }

            // 删除会话
            DeviceSessionManger.GetInstance().DeleteSession(deviceId);
        }
    }
}
