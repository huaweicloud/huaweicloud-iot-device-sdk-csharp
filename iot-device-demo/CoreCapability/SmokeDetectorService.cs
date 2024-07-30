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
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Service;
using Newtonsoft.Json.Linq;
using NLog;

namespace IoT.Device.Demo
{
    /// <summary>
    /// Smoke sensor service. Supported properties: alarm flag, smoke density, temperature, and humidity.
    /// Supported command: ring alarm
    /// </summary>
    public class SmokeDetectorService : AbstractService
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        public const string ServiceId = "smokeDetector";
        public SmokeDetectorService()
        {
            this.SetDeviceService(this);
        }

        public override string GetServiceId()
        {
            return ServiceId;
        }

        // Defines properties based on the product model. Note that the property name and type must be the same as those defined in the product model.
        [Property(Name = "alarm", Writeable = true)]
        public int SmokeAlarm { get; set; } = 1;

        [Property(Name = "smokeConcentration", Writeable = false)]
        public float Concentration
        {
            get { return (float)new Random().NextDouble(); }
        }

        [Property(Name = "humidity", Writeable = false)]
        public int Humidity { get; set; }

        [Property(Name = "temperature", Writeable = false)]
        public float Temperature { get; set; }

        /// <summary>
        /// Defines a command. Note that the input parameters and return value types of the method are fixed and cannot be modified. Otherwise, a runtime error occurs.
        /// The method name must be the same as the command name defined in the product model.
        /// </summary>
        /// <param name="jsonParas"></param>
        /// <returns></returns>
        [DeviceCommand(Name = "ringAlarm")]
        public CommandRsp Alarm(string jsonParas)
        {
            JObject obj = JObject.Parse(jsonParas);
            int value = (int)obj["duration"];
            LOG.Info("alarm for {} seconds", value);
            return new CommandRsp(0);
        }
    }
}