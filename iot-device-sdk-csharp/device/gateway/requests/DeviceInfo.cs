/*Copyright (c) <2020>, <Huawei Technologies Co., Ltd>
 * All rights reserved.
 * &Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice, this list of
 * conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list
 * of conditions and the following disclaimer in the documentation and/or other materials
 * provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used
 * to endorse or promote products derived from this software without specific prior written permission.
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 *
 * */

using System.Text;
using Newtonsoft.Json;

namespace IoT.SDK.Device.Gateway.Requests
{
    /// <summary>
    /// 设备信息
    /// </summary>
    public class DeviceInfo
    {
        [JsonProperty("node_id")]
        public string nodeId { get; set; }
        
        [JsonProperty("device_id")]
        public string deviceId { get; set; }

        [JsonProperty("parent_device_id")]
        private string parent { get; set; }

        private string name { get; set; }

        private string description { get; set; }
        
        [JsonProperty("manufacturer_id")]
        private string manufacturerId { get; set; }

        private string model { get; set; }
        
        [JsonProperty("product_id")]
        private string productId { get; set; }
        
        [JsonProperty("fw_version")]
        private string fwVersion { get; set; }
        
        [JsonProperty("sw_version")]
        private string swVersion { get; set; }

        private string status { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DeviceInfo{");
            sb.Append("parent='" + parent + '\'');
            sb.Append(", nodeId='" + nodeId + '\'');
            sb.Append(", deviceId='" + deviceId + '\'');
            sb.Append(", name='" + name + '\'');
            sb.Append(", description='" + description + '\'');
            sb.Append(", manufacturerId='" + manufacturerId + '\'');
            sb.Append(", model='" + model + '\'');
            sb.Append(", productId='" + productId + '\'');
            sb.Append(", fwVersion='" + fwVersion + '\'');
            sb.Append(", swVersion='" + swVersion + '\'');
            sb.Append(", status='" + status + '\'');
            sb.Append("}");
            return sb.ToString();
        }
    }
}
