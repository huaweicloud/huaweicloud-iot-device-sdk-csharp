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

using IoT.SDK.Bridge.Listener;
using IoT.SDK.Bridge.Clent;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Bridge.Request;
using Newtonsoft.Json;
using NLog;

namespace IoT.Device.Demo.HubDevice.Bridge
{
    class DownLinkListener : BridgeDeviceMessageListener, BridgeCommandListener, BridgeDeviceDisConnListener
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly BridgeClient bridgeClient;

        private readonly ISessionManager sessionManager;

        public DownLinkListener(BridgeClient bridgeClient, ISessionManager sessionManager)
        {
            this.bridgeClient = bridgeClient;
            this.sessionManager = sessionManager;
        }

        public void OnDeviceMessage(string deviceId, DeviceMessage deviceMessage)
        {
        }

        public void OnCommand(string deviceId, string requestId, BridgeCommand bridgeCommand)
        {
            LOG.Info("onCommand deviceId={0}, requestId={1}, bridgeCommand={2}", deviceId, requestId, bridgeCommand);
            if (!sessionManager.GetSession(deviceId, out var session))
            {
                LOG.Warn("device={0} session is null", deviceId);
                return;
            }

            // Session.RequestIdCache.GetInstance().SetRequestId(session.deviceId, flowNo.ToString(), requestId);

            session.channel.WriteAndFlushAsync(
                $"requestId:{requestId}, command:{JsonConvert.SerializeObject(bridgeCommand.command)}\n");
        }


        public void OnDisConnect(string deviceId)
        {
            // 关闭session
            if (sessionManager.GetSession(deviceId, out var session))
            {
                session.channel.CloseAsync();
            }
        }
    }
}