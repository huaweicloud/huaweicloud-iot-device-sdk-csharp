/*
 * Copyright (c) 2023-2023 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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

namespace IoT.Device.Demo
{
    /// <summary>
    /// Smoke sensor service. Supported properties: alarm flag, smoke density, temperature, and humidity.
    /// Supported command: ring alarm
    /// </summary>
    public class SmokeDetectorService : AbstractService
    {
        public SmokeDetectorService()
        {
            this.SetDeviceService(this);
        }

        // Defines propreties based on the product model. Note that the property name and type must be the same as those defined in the product model.
        [Property(Name = "alarm", Writeable = true)]
        public int smokeAlarm { get; set; } = 1;

        [Property(Name = "smokeConcentration", Writeable = false)]
        public float concentration
        {
            get
            {
                return (float)new Random().NextDouble();
            }
        }

        [Property(Writeable = false)]
        public int humidity { get; set; }
        
        [Property(Writeable = false)]
        public float temperature
        {
            get
            {
                // Simulate data reading from a sensor.
                return (float)new Random().NextDouble();
            }
        }

        /// <summary>
        /// Defines a command. Note that the input parameters and return value types of the method are fixed and cannot be modified. Otherwise, a runtime error occurs.
        /// The method name must be the same as the command name defined in the product model.
        /// </summary>
        /// <param name="jsonParas"></param>
        /// <returns></returns>
        [DeviceCommand(Name = "ringAlarm")]
        public CommandRsp alarm(string jsonParas)
        {
            JObject obj = JObject.Parse(jsonParas);
            int value = (int)obj["value"];
            
            return new CommandRsp(0);
        }
    }
}
