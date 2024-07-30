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

using System.Collections.Generic;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Transport;
using NLog;

namespace IoT.Device.Demo
{
    public class PropertyReportSample : DeviceSample, MessagePublishListener
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        protected override void BeforeInitDevice()
        {
            Device.GetClient().messagePublishListener = this;
        }

        protected override void RunDemo()
        {
            var serviceProperty = new ServiceProperty
            {
                properties = new Dictionary<string, object>
                {
                    // Sets properties based on the product model.
                    { "alarm", 1 },
                    { "temperature", 23.45812 },
                    { "humidity", 56.89013 },
                    { "smokeConcentration", 89.5672 }
                },
                serviceId = "smokeDetector" // The serviceId must be the same as that defined in the product model.
            };

            Device.GetClient().ReportProperties(new List<ServiceProperty> { serviceProperty });
        }

        public void OnMessagePublished(RawMessage message)
        {
            LOG.Info("pubSuccessMessage: {}", message.Payload);
        }

        public void OnMessageUnPublished(RawMessage message)
        {
            LOG.Info("pubFailMessage: {}", message.Payload);
        }
    }
}