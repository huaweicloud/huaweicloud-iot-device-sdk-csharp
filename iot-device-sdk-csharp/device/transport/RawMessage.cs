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

namespace IoT.SDK.Device.Transport
{
    /// <summary>
    /// 原始消息类
    /// </summary>
    public class RawMessage
    {
        /// <summary>
        /// Initializes a new instance of the RawMessage class.
        /// </summary>
        /// <param name="topic">消息topic</param>
        /// <param name="payload">消息体</param>
        public RawMessage(string topic, string payload)
        {
            this.Topic = topic;
            this.Payload = payload;
            this.Qos = 1;
        }

        /// <summary>
        /// Initializes a new instance of the RawMessage class.
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <param name="topic">消息topic</param>
        /// <param name="payload">消息体</param>
        public RawMessage(string messageId, string topic, string payload)
        {
            this.MessageId = messageId;
            this.Topic = topic;
            this.Payload = payload;
            this.Qos = 1;
        }

        /// <summary>
        /// Initializes a new instance of the RawMessage class.
        /// </summary>
        /// <param name="topic">消息topic</param>
        /// <param name="payload">消息体</param>
        /// <param name="qos">qos,0或1</param>
        public RawMessage(string topic, string payload, int qos)
        {
            this.Qos = qos;
            this.Topic = topic;
            this.Payload = payload;
        }

        public RawMessage(string messageId)
        {
            this.MessageId = messageId;
        }

        /// <summary>
        /// 消息主题
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// 消息体
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// qos,0或1，默认为1
        /// </summary>
        public int Qos { get; set; }
        
        /// <summary>
        /// 唯一消息Id
        /// </summary>
        public string MessageId { get; set; }
        
        public override string ToString()
        {
            return this.Payload;
        }
    }
}
