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
using System.Linq;
using System.Text;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Utils;
using Newtonsoft.Json;

namespace IoT.SDK.Device.Client.Requests
{
    /// <summary>
    /// Provides APIs related to device messages.
    /// </summary>
    public class RawDeviceMessage
    {
        private static readonly HashSet<string> SYSTEM_MESSAGE_KEYS = new HashSet<string>
            { "name", "id", "content", "object_device_id" };

        /// <summary>
        /// Default constructor used to create a RawDeviceMessage object.
        /// </summary>
        public RawDeviceMessage()
        {
        }

        /// <summary>
        /// Constructor used to create a RawDeviceMessage object.
        /// </summary>
        /// <param name="payload">Indicates the original received payload.</param>
        /// <param name="mqttV5Data"></param>
        public RawDeviceMessage(byte[] payload, MqttV5Data mqttV5Data = null)
        {
            this.payload = payload;
            this.MqttV5Data = mqttV5Data;
        }

        public RawDeviceMessage(string payload, MqttV5Data mqttV5Data = null)
        {
            this.payload = Encoding.UTF8.GetBytes(payload);
            this.MqttV5Data = mqttV5Data;
        }

        /// <summary>
        /// Indicates the  original received payload.
        /// </summary>
        public byte[] payload { get; set; }


        /// <summary>
        /// Indicates the mqtt v5 data
        /// </summary>
        public MqttV5Data MqttV5Data { get; set; }


        /// <summary>
        /// use <see cref="payload"/> to get raw bytes.
        /// </summary>
        public string ToUtf8String()
        {
            return Encoding.UTF8.GetString(payload);
        }

        public DeviceMessage ToDeviceMessage()
        {
            try
            {
                Dictionary<string, object> d = JsonUtil.ConvertJsonStringToDic<string, object>(ToUtf8String());
                return d.Keys.Any(dKey => !SYSTEM_MESSAGE_KEYS.Contains(dKey))
                    ? null
                    : JsonUtil.ConvertDicToObject<DeviceMessage>(d);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}