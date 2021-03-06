﻿/*Copyright (c) <2020>, <Huawei Technologies Co., Ltd>
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

using System.Collections.Generic;
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Client.Requests;

namespace IoT.SDK.Device.Service
{
    /// <summary>
    /// 服务接口类
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// 读属性回调
        /// </summary>
        /// <param name="fields">指定读取的字段名，不指定则读取全部可读字段</param>
        /// <returns>属性值，json格式</returns>
        Dictionary<string, object> OnRead(params string[] fields);

        /// <summary>
        /// 写属性回调
        /// </summary>
        /// <param name="properties">属性期望值</param>
        /// <returns>操作结果jsonObject</returns>
        IotResult OnWrite(Dictionary<string, object> properties);

        /// <summary>
        /// 命令回调
        /// </summary>
        /// <param name="command">命令</param>
        /// <returns>执行结果</returns>
        CommandRsp OnCommand(Command command);

        /// <summary>
        /// 事件回调
        /// </summary>
        /// <param name="deviceEvent">事件</param>
        void OnEvent(DeviceEvent deviceEvent);
    }
}
