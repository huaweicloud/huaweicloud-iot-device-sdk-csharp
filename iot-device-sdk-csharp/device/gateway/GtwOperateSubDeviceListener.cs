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

using IoT.SDK.Device.Gateway.response;
using IoT.SDK.Device.Gateway.Response;

namespace IoT.SDK.Device.Gateway
{
    public interface GtwOperateSubDeviceListener
    {
        /// <summary>
        ///处理网关增加子设备返回结果
        /// </summary>
        /// <param name="gtwAddSubDeviceRsp">网关增加子设备响应</param>
        /// <param name="eventId"> 事件Id</param>
        void OnAddSubDeviceRsp(GtwAddSubDeviceRsp gtwAddSubDeviceRsp, string eventId);


        /// <summary>
        ///处理网关删除子设备返回结果
        /// </summary>
        /// <param name="gtwDelSubDeviceRsp">网关删除子设备响应</param>
        /// <param name="eventId"> 事件Id</param>
        void OnDelSubDeviceRsp(GtwDelSubDeviceRsp gtwDelSubDeviceRsp, string eventId);
    }
}