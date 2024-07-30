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
using IoT.SDK.Device.Gateway.Requests;

namespace IoT.Device.Demo.Core
{
    public class GatewayConfig
    {
        public int ListenPort { get; set; }
        public List<string> PreDeleteSubDeviceIds { get; set; }
        public List<DeviceInfo> PreAddSubDevice { get; set; }
    }
    
    public class BridgeConfig
    {
        public int ListenPort { get; set; }
    }

    public class BootstrapConfig
    {
        public string ScopeId { get; set; }
        public string ReportedData { get; set; }
    }

    public class DemoConfig
    {
        public string DemoName { get; set; }
        public DemoAuthConfig AuthConfig { get; set; }
        public GatewayConfig GatewayConfig { get; set; }
        
        public BridgeConfig BridgeConfig { get; set; }
        public BootstrapConfig BootstrapConfig { get; set; }
    }

    public class DemoAuthConfig
    {
        public bool AuthUseCert { get; set; }
        public string ServerAddress { get; set; }
        public int ServerPort { get; set; }
        public string DeviceId { get; set; }
        public string DeviceSecret { get; set; }
        public string DeviceCert { get; set; }
        public string DeviceCertPassword { get; set; }
    }
}