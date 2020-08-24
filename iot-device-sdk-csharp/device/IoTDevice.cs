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

using System.Security.Cryptography.X509Certificates;
using IoT.SDK.Device.Service;

namespace IoT.SDK.Device
{
    public class IoTDevice : AbstractDevice
    {
        /// <summary>
        /// 构造函数，使用密码创建设备
        /// </summary>
        /// <param name="serverUri">平台访问地址，比如iot-mqtts.cn-north-4.myhuaweicloud.com</param>
        /// <param name="port">端口</param>
        /// <param name="deviceId">设备id</param>
        /// <param name="deviceSecret">设备密码</param>
        public IoTDevice(string serverUri, int port, string deviceId, string deviceSecret) : base(serverUri, port, deviceId, deviceSecret)
        {
        }

        /// <summary>
        /// X509证书接入
        /// </summary>
        /// <param name="serverUri">平台访问地址，比如iot-mqtts.cn-north-4.myhuaweicloud.com</param>
        /// <param name="port">端口</param>
        /// <param name="deviceId">设备id</param>
        /// <param name="deviceCert">设备证书</param>
        public IoTDevice(string serverUri, int port, string deviceId, X509Certificate deviceCert) : base(serverUri, port, deviceId, deviceCert)
        {
        }
    }
}
