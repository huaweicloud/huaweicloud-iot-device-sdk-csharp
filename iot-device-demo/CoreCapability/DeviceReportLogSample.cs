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
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Log;
using IoT.SDK.Device.Transport;
using NLog;

namespace IoT.Device.Demo
{
    public class DeviceReportLogSample : DeviceSample
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private class DemoMessagePublishListener : MessagePublishListener
        {
            public void OnMessagePublished(RawMessage message)
            {
                //  be careful to using device.LogService.ReportLog() or NLog in here!!! see README.md to get more detail and caution.
                LOG.Info("message published {}", message.Payload);
            }

            public void OnMessageUnPublished(RawMessage message)
            {
                //  same CAUTION as in OnMessagePublished!!
                LOG.Info("message isn't published {}", message.Payload);
            }
        }

        protected override void BeforeInitDevice()
        {
            Device.GetClient().messagePublishListener = new DemoMessagePublishListener();
        }

        /// <summary>
        ///  ensure sure you have enabled "Device CreateServer Logs" in IoTDA console.
        /// </summary>
        protected override void RunDemo()
        {
            while (!Device.LogService.CanLog())
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            Device.LogService.ReportLog(DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                LogService.LogType.DeviceStatus, "ONLINE");

            // use Nog with "IoTDA" target and decent filter configured in NLog.config.
            // see README.md to get more detail。
            LOG.Info("report to cloud");
        }
    }
}