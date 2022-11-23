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
using IoT.SDK.Bridge.Clent;
using IoT.SDK.Bridge.Bootstrap;
using IoT.Bridge.Sample.Tcp.Handler;

namespace IoT.Bridge.Sample.Tcp.Bridge {
    class BridgeService {
        private static BridgeClient bridgeClient;

        public void Init()
        {
            // 网桥启动初始化
            BridgeBootstrap bridgeBootstrap = new BridgeBootstrap();

            // 从环境变量获取配置进行初始化
            bridgeBootstrap.InitBridge();

            bridgeClient = bridgeBootstrap.GetBridgeDevice().bridgeClient;

            // 设置平台下行数据监听器
            DownLinkHandler downLinkHandler = new DownLinkHandler(bridgeClient);
            bridgeClient.bridgeCommandListener = downLinkHandler;   // 设置平台命令下发监听器
            bridgeClient.bridgeDeviceMessageListener = downLinkHandler;    // 设置平台消息下发监听器
            bridgeClient.bridgeDeviceDisConnListener = downLinkHandler;   // 设置平台通知网桥主动断开设备连接的监听器
        }

        public static BridgeClient GetBridgeClient()
        {
            return bridgeClient;
        }
    }
}
