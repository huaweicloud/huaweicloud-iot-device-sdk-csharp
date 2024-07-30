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
using Newtonsoft.Json;

namespace IoT.SDK.Device.Client.Requests
{
    public class DeviceShadowServiceProperty
    {
        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }

        /// <summary>
        /// Indicates the time when the property value was changed, in the format of yyyyMMddTHHmmssZ. It is optional. If it is set to NULL, the time when the platform received the property value is used.
        /// </summary>
        [JsonProperty("event_time")]
        public string EventTime { get; set; }
    }

    public class DeviceShadow
    {
        [JsonProperty("service_id")]
        public string ServiceId { get; set; }

        [JsonProperty("desired")]
        public DeviceShadowServiceProperty Desired { get; set; }

        [JsonProperty("reported")]
        public DeviceShadowServiceProperty Reported { get; set; }


        [JsonProperty("version")]
        public long Version { get; set; }
    }

    public class DeviceShadowResponse
    {
        [JsonProperty("object_device_id")]
        public string DeviceId { get; set; }

        [JsonProperty("shadow")]
        public List<DeviceShadow> Shadow { get; set; }
    }


    public class DeviceShadowRequest
    {
        [JsonProperty("object_device_id")]
        public string DeviceId { get; set; }

        [JsonProperty("service_id")]
        public string ServiceId { get; set; }
    }
}