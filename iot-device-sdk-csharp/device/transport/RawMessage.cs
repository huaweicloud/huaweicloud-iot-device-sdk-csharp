/*
 * Copyright (c) 2020-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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

using System.Text;

namespace IoT.SDK.Device.Transport
{
    /// <summary>
    /// Provides APIs related to raw messages.
    /// </summary>
    public class RawMessage
    {
        /// <summary>
        /// Initializes a new instance of the RawMessage class.
        /// </summary>
        /// <param name="topic">Indicates a message topic.</param>
        /// <param name="payload">Indicates the message body.</param>
        public RawMessage(string topic, string payload)
        {
            this.Topic = topic;
            this.Payload = payload;
            this.Qos = 1;
        }

        
        /// <summary>
        /// Initializes a new instance of the RawMessage class.
        /// </summary>
        /// <param name="topic">Indicates a message topic.</param>
        /// <param name="payload">Indicates the message body.</param>
         public RawMessage(string topic, byte[] payload)
        {
            this.Topic = topic;
            this.BinPayload = payload;
            this.Qos = 1;
        }
        
        /// <summary>
        /// Initializes a new instance of the RawMessage class.
        /// </summary>
        /// <param name="messageId">Indicates a message ID.</param>
        /// <param name="topic">Indicates a message topic.</param>
        /// <param name="payload">Indicates the message body.</param>
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
        /// <param name="topic">Indicates a topic.</param>
        /// <param name="payload">Indicates the message body.</param>
        /// <param name="qos">Indicates a QoS level. The value can be 0 or 1.</param>
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
        /// Indicates a message topic.
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// Indicates a message body in binary format.
        /// </summary>
        public byte[] BinPayload { get; set; }

        /// <summary>
        /// Indicates a message body.
        /// </summary>
        public string Payload
        {
            get => Encoding.UTF8.GetString(BinPayload);
            set => BinPayload = Encoding.UTF8.GetBytes(value);
        }

        /// <summary>
        /// Indicates a QoS level. The value can be 0 or 1. The default value is 1.
        /// </summary>
        public int Qos { get; set; }

        /// <summary>
        /// Indicates a message ID.
        /// </summary>
        public string MessageId { get; set; }

        public override string ToString()
        {
            return this.Payload;
        }

        public MqttV5Data MqttV5Data { get; set; }
    }
}