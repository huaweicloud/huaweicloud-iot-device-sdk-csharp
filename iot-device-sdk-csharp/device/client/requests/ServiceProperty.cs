/*
 * Copyright (c) 2020-2020 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Text;
using Newtonsoft.Json;

namespace IoT.SDK.Device.Client.Requests
{
    /// <summary>
    /// 服务的属性
    /// </summary>
    public class ServiceProperty
    {
        /// <summary>
        /// 服务id，和设备模型里一致
        /// </summary>
        [JsonProperty("service_id")]
        public string serviceId { get; set; }

        /// <summary>
        /// 属性值，具体字段由设备模型定义
        /// </summary>
        public Dictionary<string, object> properties { get; set; }

        /// <summary>
        /// 属性变化的时间，格式：yyyyMMddTHHmmssZ，可选，不带以平台收到的时间为准
        /// </summary>
        public string eventTime { get; set; }
        
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ServiceProperty{");
            sb.Append("serviceId='");
            sb.Append(serviceId);
            sb.Append("\'");
            sb.Append(", properties=");
            sb.Append(properties);
            sb.Append(", eventTime='");
            sb.Append(eventTime);
            sb.Append("\'}");
            return sb.ToString();
        }
    }
}
