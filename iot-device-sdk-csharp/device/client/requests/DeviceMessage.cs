/*
 * Copyright (c) 2020-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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

using IoT.SDK.Device.Transport;
using Newtonsoft.Json;

namespace IoT.SDK.Device.Client.Requests
{
    /// <summary>
    /// Provides APIs related to device messages.
    /// </summary>
    public class DeviceMessage
    {
        /// <summary>
        /// Default constructor used to create a DeviceMessage object.
        /// </summary>
        public DeviceMessage()
        {
        }

        /// <summary>
        /// Constructor used to create a DeviceMessage object.
        /// </summary>
        /// <param name="message">Indicates the message content.</param>
        public DeviceMessage(string message, MqttV5Data mqttV5Data = null)
        {
            content = message;
            this.MqttV5Data = mqttV5Data;
        }

        /// <summary>
        /// Indicates the message content.
        /// </summary>
        public string content { get; set; }

        /// <summary>
        /// Indicates a device ID. It is optional. The default value is the device ID of the client.
        /// </summary>
        [JsonProperty("object_device_id")]
        public string deviceId { get; set; }

        /// <summary>
        /// Indicates a message name. It is optional.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Indicates a message ID. It is optional.
        /// </summary>
        public string id { get; set; }
        
        [JsonIgnore]
        public MqttV5Data MqttV5Data { get; set; }
    }
}