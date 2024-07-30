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

using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using NLog;

namespace IoT.Device.Demo
{
    /// <summary>
    /// Obtains device shadow data.
    /// </summary>
    public class DeviceShadowSample : DeviceSample, DeviceShadowListener
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        protected override void BeforeInitDevice()
        {
            Device.GetClient().deviceShadowListener = this;
        }

        protected override void RunDemo()
        {
            var reqId = Device.GetClient().GetShadow(new DeviceShadowRequest
            {
                ServiceId = null // service id in you product model, for example, "SmokeDetector"
            });
            LOG.Info("get shadow request id: {}", reqId);
        }

        public void OnShadowCommand(string requestId, string message)
        {
            LOG.Info("receive device shadow, request id:{}, message:{}", requestId, message);
        }
    }
}