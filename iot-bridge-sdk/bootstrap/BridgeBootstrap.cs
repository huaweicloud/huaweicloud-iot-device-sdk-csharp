/*
 * Copyright (c) 2022-2022 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Collections.Generic;
using System.Text;
using IoT.SDK.Bridge.Device;
using IoT.SDK.Device.Client;
using NLog;

namespace IoT.SDK.Bridge.Bootstrap {
    // 网桥启动初始化
    public class BridgeBootstrap {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // 网桥模式
        private static readonly int CONNECT_OF_BRIDGE_MODE = 3;

        private BridgeDevice bridgeDevice;

        // 从环境变量获取网桥配置信息，初始化网桥。
        public void InitBridge()
        {
            BridgeClientConf conf = BridgeClientConf.Config();
            InitBridge(conf);
        }

        // 根据网桥配置信息，初始化网桥
        public void InitBridge(BridgeClientConf conf)
        {
            if (conf == null) {
                conf = BridgeClientConf.Config();
            }
            BridgeOnline(conf);
        }

        private void BridgeOnline(BridgeClientConf conf)
        {
            ClientConf clientConf = new ClientConf();
            clientConf.ServerUri = conf.serverIp;
            clientConf.Port = conf.serverPort;
            clientConf.DeviceId = conf.bridgeId;
            clientConf.Secret = conf.bridgeSecret;
            clientConf.Mode = CONNECT_OF_BRIDGE_MODE;

            BridgeDevice bridgeDev = BridgeDevice.GetInstance(clientConf);
            if (bridgeDev.Init() != 0) {
                Log.Error("Bridge can't login. please check!");
            }
            this.bridgeDevice = bridgeDev;
        }

        public BridgeDevice GetBridgeDevice()
        {
            return bridgeDevice;
        }
    }
}
