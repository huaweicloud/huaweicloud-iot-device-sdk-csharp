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

using System.Collections.Generic;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Service.DeviceRule;
using NLog;

namespace IoT.Device.Demo
{
    /*
     * try set a rule that execute command when  15 > "Temperature" > 10
     */
    public class DeviceRuleSample : DeviceSample
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private class DemoDeviceRuleCommandListener : DeviceRuleCommandListener
        {
            public void OnDeviceRuleCommand(string requestId, Command command)
            {
                LOG.Info("get command sent to another device:{}", command);
            }
        }

        protected override void BeforeInitDevice()
        {
            var smokeDetectorService = new SmokeDetectorService();
            Device.AddService(smokeDetectorService);
            Device.DeviceRuleService.DeviceRuleStoragePath = "rule.json";
            Device.DeviceRuleService.EnableDeviceRule = true;
            Device.DeviceRuleService.OnRuleUpdated = TestPropertiesCondition;
            // command that sent to another device
            Device.DeviceRuleService.DeviceRuleCommandListener = new DemoDeviceRuleCommandListener();
        }


        private void TestPropertiesCondition(string s)
        {
            if (!Device.GetClient().IsConnected())
            {
                return;
            }

            var smokeDetectorService = (SmokeDetectorService)Device.GetService(SmokeDetectorService.ServiceId);
            for (var i = 0; i < 20; ++i)
            {
                smokeDetectorService.Temperature = i;
                smokeDetectorService.Humidity = i;
                LOG.Info("temperature is {}", smokeDetectorService.Temperature);
                smokeDetectorService.FirePropertiesChanged();
            }
        }

        protected override void RunDemo()
        {
        }
    }
}