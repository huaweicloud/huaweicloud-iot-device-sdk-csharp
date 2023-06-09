/*
 * Copyright (c) 2020-2020 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Client.Requests;

namespace IoT.SDK.Device.Service
{
    /// <summary>
    /// Provides APIs related to services.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Called when a property read request is received.
        /// </summary>
        /// <param name="fields">Indicates the names of fields to read. If it is set to NULL, all readable fields are read.</param>
        /// <returns>Returns the property values, in JSON format.</returns>
        Dictionary<string, object> OnRead(params string[] fields);

        /// <summary>
        /// Called when a property write request is received.
        /// </summary>
        /// <param name="properties">Indicates the desired properties.</param>
        /// <returns>Returns the operation result, which is a JSON object.</returns>
        IotResult OnWrite(Dictionary<string, object> properties);

        /// <summary>
        /// Called when a command delivered by the platform is received.
        /// </summary>
        /// <param name="command">Indicates a command request.</param>
        /// <returns>Returns a command response.</returns>
        CommandRsp OnCommand(Command command);

        /// <summary>
        /// Called when an event delivered by the platform is received.
        /// </summary>
        /// <param name="deviceEvent">Indicates the event.</param>
        void OnEvent(DeviceEvent deviceEvent);
    }
}
