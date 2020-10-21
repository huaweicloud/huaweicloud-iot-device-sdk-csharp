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

using System.Collections.Generic;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Utils;

namespace IoT.SDK.Device.Transport
{
    public class PubMessage
    {
        /// <summary>
        /// 上报设备属性（reportProperties）
        /// </summary>
        /// <param name="properties">设备属性列表</param>
        public PubMessage(List<ServiceProperty> properties)
        {
            this.Topic = CommonTopic.TOPIC_PROPERTIES_REPORT;
            this.Message = "{\"services\":" + JsonUtil.ConvertObjectToJsonString(properties) + "}";
        }

        public PubMessage(string topic, List<ServiceProperty> properties)
        {
            this.Topic = topic;
            this.Message = "{\"services\":" + JsonUtil.ConvertObjectToJsonString(properties) + "}";
        }

        public PubMessage(string topic, DeviceProperties deviceProperties)
        {
            this.Topic = topic;
            this.Message = "{\"services\":" + JsonUtil.ConvertObjectToJsonString(deviceProperties) + "}";
        }

        /// <summary>
        /// 发布原始消息，原始消息和设备消息（DeviceMessage）的区别是：
        /// 1、可以自定义topic，该topic需要在平台侧配置
        /// 2、不限制payload的格式
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message">原始消息</param>
        public PubMessage(string topic, string message)
        {
            this.Topic = topic;
            this.Message = message;
        }

        /// <summary>
        /// 上报设备消息（reportDeviceMessage）
        /// 如果需要上报子设备消息，需要调用DeviceMessage的setDeviceId接口设置为子设备的设备id
        /// </summary>
        /// <param name="deviceMessage"></param>
        public PubMessage(DeviceMessage deviceMessage)
        {
            this.Topic = CommonTopic.TOPIC_MESSAGES_UP;
            this.Message = deviceMessage.content;
        }

        /// <summary>
        /// 上报命令响应（respondCommand）
        /// </summary>
        /// <param name="requestId">请求id，响应的请求id必须和请求的一致</param>
        /// <param name="commandRsp">命令响应</param>
        public PubMessage(string requestId, CommandRsp commandRsp)
        {
            this.Topic = CommonTopic.TOPIC_COMMANDS_RESPONSE + "=" + requestId;
            this.Message = JsonUtil.ConvertObjectToJsonString(commandRsp);
        }

        public PubMessage(string topic, DeviceEvents events)
        {
            this.Topic = topic;
            this.Message = JsonUtil.ConvertObjectToJsonString(events);
        }

        public string Topic { get; set; }

        public string Message { get; set; }
    }
}
