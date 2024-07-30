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

using System.Security.Cryptography.X509Certificates;
using MQTTnet.Protocol;

namespace IoT.SDK.Device.Client
{
    public class ClientConf
    {
        // Directly connected device access mode
        public static readonly int CONNECT_OF_NORMAL_DEVICE_MODE = 0;

        // Bridge Device Access Mode
        public static readonly int CONNECT_OF_BRIDGE_MODE = 3;

        /// <summary>
        /// enable mqtt v5, don't use this for bootstrap
        /// </summary>
        public bool UseMqttV5 { get; set; } = true;

        /// <summary>
        /// Indicates a device ID, which is obtained when the device is registered on the platform.
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Indicates a secret.
        /// </summary>
        public string Secret { get; set; }

        /// <summary>
        /// Indicates a device access address.
        /// </summary>
        public string ServerUri { get; set; }

        /// <summary>
        /// Indicates a port for device access.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Device access mode, the Default value is 0.
        /// </summary>
        public int Mode { get; set; }

        /// <summary>
        /// Indicates the QoS level. The default value is 1.
        /// </summary>
        public MqttQualityOfServiceLevel Qos { get; set; } = MqttQualityOfServiceLevel.AtLeastOnce;

        /// <summary>
        /// Indicates the device certificate. It is used to authenticate the device in the case of certification authentication.
        /// </summary>
        public X509Certificate DeviceCert { get; set; }

        /// <summary>
        /// iot平台的ca证书存放路径，用于设备侧校验平台
        ///  CA of IoT platform, which is used by device client to verify server. 
        /// </summary>
        public X509Certificate IotCaCert { get; set; }

        /// <summary>
        /// Indicates the scope ID. It is used in the group self-registration scenario during device provisioning.
        /// </summary>
        public string ScopeId { get; set; }

        /// <summary>
        /// Indicates whether check timestamp, "0" means not check, "1" means check.
        /// </summary>
        public string CheckTimestamp { get; set; } = "1";


        /// <summary>
        /// Indicates whether check timestamp, "0" means not check, "1" means check.
        /// </summary>
        public bool AutoReconnect { get; set; } = true;
    }
}