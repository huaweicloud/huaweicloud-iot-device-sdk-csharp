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

using System.Text;
using Newtonsoft.Json;

namespace IoT.SDK.Device.OTA
{
    public class OTAPackage
    {
        public string url { get; set; }

        public string version { get; set; }

        [JsonProperty("file_size")]
        public int fileSize { get; set; }

        [JsonProperty("file_name")]
        public string fileName { get; set; }

        [JsonProperty("access_token")]
        public string token { get; set; }

        public int expires { get; set; }

        public string sign { get; set; }

        [JsonProperty("custom_info")]
        public string customInfo { get; set; }

        [JsonProperty("task_id")]
        public string taskId { get; set; }

        [JsonProperty("sub_device_count")]
        public int subDeviceCount { get; set; }

        [JsonProperty("task_ext_info")]
        public object taskExtInfo { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("OTAPackage{");
            sb.Append("url='" + url + '\'');
            sb.Append(", version='" + version + '\'');
            sb.Append(", fileSize=" + fileSize);
            sb.Append(", fileName='" + fileName + '\'');
            sb.Append(", token='" + token + '\'');
            sb.Append(", expires=" + expires);
            sb.Append(", sign='" + sign + '\'');
            sb.Append(", customInfo='" + customInfo + '\'');
            sb.Append(", taskId='" + taskId + '\'');
            sb.Append(", subDeviceCount=" + subDeviceCount);
            sb.Append(", taskExtInfo='" + taskExtInfo + '\'');
            sb.Append('}');
            return sb.ToString();
        }
    }
}
