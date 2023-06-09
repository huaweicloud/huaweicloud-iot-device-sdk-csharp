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
using MQTTnet;

namespace IoT.SDK.Device.Transport
{
    internal interface Connection
    {
        /// <summary>
        /// Creates a connection.
        /// </summary>
        /// <returns>Returns 0 if the connection is established; returns other values if the connection fails to be established.</returns>
        int Connect();

        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <param name="message">Indicates the message to publish.</param>
        void PublishMessage(RawMessage message);

        /// <summary>
        /// Closes the connection.
        /// </summary>
        void Close();

        /// <summary>
        /// Checks whether the device is connected to the platform.
        /// </summary>
        /// <returns>Returns true if the device is connected to the platform; returns false otherwise.</returns>
        bool IsConnected();

        /// <summary>
        /// Sets a connection listener.
        /// </summary>
        /// <param name="connectListener">Indicates the listener to set.</param>
        void SetConnectListener(ConnectListener connectListener);

        /// <summary>
        /// Subscribes to a topic.
        /// </summary>
        /// <param name="listTopic">Indicates the custom topic to subscribe. The SDK automatically subscribes to system topics.</param>
        void SubscribeTopic(List<MqttTopicFilter> listTopic);
    }
}
