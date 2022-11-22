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
using IoT.SDK.Device;
using IoT.SDK.Device.Client;
using IoT.SDK.Bridge.Clent;
using NLog;

namespace IoT.SDK.Bridge.Device {
    public class BridgeDevice : IoTDevice {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static BridgeDevice instance;

        public BridgeClient bridgeClient { get; }

        private BridgeDevice(ClientConf clientConf) : base(clientConf.ServerUri, clientConf.Port, clientConf.DeviceId, clientConf.Secret)
        {
            if (clientConf.Mode != ClientConf.CONNECT_OF_BRIDGE_MODE) {
                throw new Exception("the bridge mode is invalid which the value should be 3.");
            }
            bridgeClient = new BridgeClient(clientConf, this);
        }

        // 此处采用单例模式，默认一个网桥服务，只会启动一个网桥，且网桥参数一致
        public static BridgeDevice GetInstance(ClientConf clientConf)
        {
            if (instance == null) {
                instance = new BridgeDevice(clientConf);
            }
            return instance;
        }

        public new int Init()
        {
            Log.Debug("the bridge client starts to init.");
            return bridgeClient.Connect();
        }

        public override DeviceClient GetClient()
        {
            return bridgeClient;
        }
    
    }
}
