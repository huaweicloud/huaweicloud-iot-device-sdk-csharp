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

using System;
using IoT.SDK.Device.Client.Requests;
using NLog;

namespace IoT.SDK.Device.Service
{
    /// <summary>
    /// 抽象服务类，提供了属性自动读写和命令调用能力，用户可以继承此类，根据物模型定义自己的服务
    /// </summary>
    public class AbstractService : IService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public AbstractDevice iotDevice { get; set; }

        public string ServiceId { get; set; }

        public CommandRsp OnCommand(Command command)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 事件处理。收到平台下发的事件时此接口被自动调用。默认为空实现
        /// </summary>
        /// <param name="deviceEvent"></param>
        public virtual void OnEvent(DeviceEvent deviceEvent)
        {
            Log.Info("onEvent no op");
        }
    }
}
