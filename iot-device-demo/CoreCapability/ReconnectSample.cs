/*
 * Copyright (c) 2024-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Threading;
using System.Threading.Tasks;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Transport;

namespace IoT.Device.Demo
{
    public class ReconnectSample : DeviceSample, ConnectListener
    {
        protected override void BeforeInitDevice()
        {
            // Creates a service.
            Device.GetClient().connectListener = this;
        }

        protected override void RunDemo()
        {
            // 创建一个定时器，每5秒执行一次回调函数
            _ = new Timer(
                _ =>
                {
                    if (!Device.GetClient().IsConnected()) return;
                    // 在这里报告设备消息
                    Device.GetClient().ReportRawDeviceMessage(new RawDeviceMessage("message"));
                },
                null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        public void ConnectionLost()
        {
        }

        public void ConnectComplete()
        {
            // 如果有自定义topic，请在此处重新订阅
        }

        public void ConnectFail()
        {
            Task.Run(() =>
            {
                Device.GetClient().Close();
                Device.GetClient().Connect();
            });
        }
    }
}