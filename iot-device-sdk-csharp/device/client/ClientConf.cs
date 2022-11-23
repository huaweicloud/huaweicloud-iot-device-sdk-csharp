/*
 * Copyright (c) 2020-2022 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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

using System.Security.Cryptography.X509Certificates;
using MQTTnet.Protocol;

namespace IoT.SDK.Device.Client
{
    public class ClientConf
    {
        // 直连模式
        public static readonly int CONNECT_OF_NORMAL_DEVICE_MODE = 0;

        // 网桥模式
        public static readonly int CONNECT_OF_BRIDGE_MODE = 3;

        /// <summary>
        /// 设备ID，在平台注册设备生成
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 设备密码
        /// </summary>
        public string Secret { get; set; }

        /// <summary>
        /// 设备接入平台地址
        /// </summary>
        public string ServerUri { get; set; }

        /// <summary>
        /// 设备接入平台端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 设备接入模式
        /// </summary>
        public int Mode { get; set; }
		
        /// <summary>
        /// 客户端qos,默认值为1
        /// </summary>
        public MqttQualityOfServiceLevel Qos { get; set; } = MqttQualityOfServiceLevel.AtLeastOnce;

        /// <summary>
        /// 设备证书，用于X509证书接入时校验
        /// </summary>
        public X509Certificate DeviceCert { get; set; }
    }
}
