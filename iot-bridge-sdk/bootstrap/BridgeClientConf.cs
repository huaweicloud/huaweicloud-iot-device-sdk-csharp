/*
 * Copyright (c) 2022-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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

using System;

namespace IoT.SDK.Bridge.Bootstrap
{
    public class BridgeClientConf
    {
        // 网桥ID环境变量名称
        private const string ENV_NET_BRIDGE_ID = "NET_BRIDGE_ID";

        // 网桥密码环境变量名称
        private const string ENV_NET_BRIDGE_SECRET = "NET_BRIDGE_SECRET";

        // 平台接入地址变量名称
        private const string ENV_NET_BRIDGE_SERVER_IP = "NET_BRIDGE_SERVER_IP";

        // 平台接入端口变量名称
        private const string ENV_NET_BRIDGE_SERVER_PORT = "NET_BRIDGE_SERVER_PORT";

        // 连接IoT平台的网桥ID.
        public string BridgeId { get; set; }

        // 连接IoT平台的网桥密码
        public string BridgeSecret { get; set; }

        // 连接IoT平台的地址 样例：xxxxxx.iot-mqtts.cn-north-4.myhuaweicloud.com
        public string ServerIp { get; set; }

        // 连接IoT平台的端口
        public int ServerPort { get; set; }


        public static BridgeClientConf Config()
        {
            var conf = new BridgeClientConf
            {
                ServerIp = Environment.GetEnvironmentVariable(ENV_NET_BRIDGE_SERVER_IP),
                ServerPort = int.Parse(Environment.GetEnvironmentVariable(ENV_NET_BRIDGE_SERVER_PORT) ?? "0"),
                BridgeId = Environment.GetEnvironmentVariable(ENV_NET_BRIDGE_ID),
                BridgeSecret = Environment.GetEnvironmentVariable(ENV_NET_BRIDGE_SECRET)
            };

            return conf;
        }
    }
}