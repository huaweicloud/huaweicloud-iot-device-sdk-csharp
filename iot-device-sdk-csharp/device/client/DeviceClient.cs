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
using System.Collections.Generic;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Service;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Transport.Mqtt;
using IoT.SDK.Device.Utils;
using MQTTnet;
using Newtonsoft.Json.Linq;
using NLog;

namespace IoT.SDK.Device.Client
{
    public class DeviceClient : RawMessageListener, ConnectListener
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private CommandV3Listener commandV3Listener;

        private ClientConf clientConf;

        private Connection connection;

        private string deviceId;

        private Dictionary<string, RawMessageListener> rawMessageListenerDic;

        private AbstractDevice device;
        
        public DeviceClient(ClientConf clientConf, AbstractDevice device)
        {
            this.CheckClientConf(clientConf);
            this.clientConf = clientConf;
            this.deviceId = clientConf.DeviceId;
            this.connection = new MqttConnection(clientConf, this, this);
            this.device = device;
            this.rawMessageListenerDic = new Dictionary<string, RawMessageListener>();
        }

        public CommandListener commandListener { get; set; }

        public DeviceShadowListener deviceShadowListener { get; set; }

        public DeviceCustomMessageListener deviceCustomMessageListener { get; set; }

        public MessagePublishListener messagePublishListener { get; set; }
        
        public PropertyListener propertyListener { get; set; }

        public DeviceMessageListener deviceMessageListener { get; set; }

        public ConnectListener connectListener { get; set; }

        public void OnMessageReceived(RawMessage message)
        {
            string topic = message.Topic;
            try
            {
                RawMessageListener listener = null;
                if (rawMessageListenerDic.ContainsKey(topic))
                {
                    listener = rawMessageListenerDic[topic];
                }
                
                if (listener != null)
                {
                    listener.OnMessageReceived(message);

                    return;
                }

                if (topic.Contains("/messages/down"))
                {
                    OnDeviceMessage(message);
                }
                else if (topic.Contains("sys/commands/request_id"))
                {
                    OnCommand(message);
                }
                else if (topic.Contains("/sys/properties/set/request_id"))
                {
                    OnPropertiesSet(message);
                }
                else if (topic.Contains("/sys/properties/get/request_id"))
                {
                    OnPropertiesGet(message);
                }
                else if (topic.Contains("/sys/events/down"))
                {
                    OnEvent(message);
                }
                else if (topic.Contains("/huawei/v1/devices") && topic.Contains("/command/"))
                {
                    OnCommandV3(message);
                }
                else if (topic.Contains("/sys/shadow/get/response"))
                {
                    OnShadowCommand(message);
                }
                else if (topic.Contains("/user/"))
                {
                    OnCustomCommand(message);
                }
                else
                {
                    Log.Error("unknown topic: " + topic);
                }
            }
            catch (Exception ex)
            {
                Log.Error("SDK.Error: message received error, the topic is " + topic);
            }
        }

        /// <summary>
        /// 上报命令响应
        /// </summary>
        /// <param name="requestId">请求id，响应的请求id必须和请求的一致</param>
        /// <param name="commandRsp">命令响应</param>
        public void RespondCommand(string requestId, CommandRsp commandRsp)
        {
            Report(new PubMessage(CommonTopic.TOPIC_COMMANDS_RESPONSE + "=" + requestId, JsonUtil.ConvertObjectToJsonString(commandRsp)));
        }

        /// <summary>
        /// 上报读属性响应
        /// </summary>
        /// <param name="requestId">请求id，响应的请求id必须和请求的一致</param>
        /// <param name="services">服务属性</param>
        public void RespondPropsGet(string requestId, List<ServiceProperty> services)
        {
            DeviceProperties deviceProperties = new DeviceProperties();
            deviceProperties.services = services;

            Report(new PubMessage(CommonTopic.TOPIC_SYS_PROPERTIES_GET_RESPONSE + "=" + requestId, deviceProperties));
        }

        /// <summary>
        /// 上报写属性响应
        /// </summary>
        /// <param name="requestId">请求id，响应的请求id必须和请求的一致</param>
        /// <param name="iotResult">写属性结果</param>
        public void RespondPropsSet(string requestId, IotResult iotResult)
        {
            Report(new PubMessage(CommonTopic.TOPIC_SYS_PROPERTIES_SET_RESPONSE + "=" + requestId, JsonUtil.ConvertObjectToJsonString(iotResult)));
        }

        public void OnCommand(RawMessage message)
        {
            string requestId = IotUtil.GetRequestId(message.Topic);
            
            Command command = JsonUtil.ConvertJsonStringToObject<Command>(message.ToString());
            
            if (command == null)
            {
                Log.Error("invalid command");

                return;
            }

            if (commandListener != null && (command.deviceId == null || command.deviceId == deviceId))
            {
                commandListener.OnCommand(requestId, command.serviceId, command.commandName, command.paras);

                return;
            }

            device.OnCommand(requestId, command);
        }
        
        /// <summary>
        /// 和平台建立连接，此接口为阻塞调用，超时时长20s。连接成功时，SDK会自动向平台订阅系统定义的topic。
        /// </summary>
        /// <returns>0表示连接成功，其他表示连接失败</returns>
        public int Connect()
        {
            int ret = connection.Connect();
            if (ret != 0)
            {
                return ret;
            }

            List<MqttTopicFilter> listTopic = new List<MqttTopicFilter>();

            var topicFilterBulderMsgDown = new MqttTopicFilterBuilder().WithTopic(string.Format(CommonTopic.TOPIC_SYS_MESSAGES_DOWN, deviceId)).Build();
            listTopic.Add(topicFilterBulderMsgDown);

            var topicFilterBulderCommand = new MqttTopicFilterBuilder().WithTopic(string.Format(CommonTopic.TOPIC_SYS_COMMAND, deviceId)).Build();
            listTopic.Add(topicFilterBulderCommand);

            var topicFilterBulderShadowGetResponse = new MqttTopicFilterBuilder().WithTopic(string.Format(CommonTopic.TOPIC_SYS_SHADOW_GET_RESPONSE, deviceId)).Build();
            listTopic.Add(topicFilterBulderShadowGetResponse);

            var topicFilterBulderPropertiesSet = new MqttTopicFilterBuilder().WithTopic(string.Format(CommonTopic.TOPIC_SYS_PROPERTIES_SET, deviceId)).Build();
            listTopic.Add(topicFilterBulderPropertiesSet);

            var topicFilterBulderPropertiesGet = new MqttTopicFilterBuilder().WithTopic(string.Format(CommonTopic.TOPIC_SYS_PROPERTIES_GET, deviceId)).Build();
            listTopic.Add(topicFilterBulderPropertiesGet);

            var topicFilterBulderEventsDown = new MqttTopicFilterBuilder().WithTopic(string.Format(CommonTopic.TOPIC_SYS_EVENTS_DOWN, deviceId)).Build();
            listTopic.Add(topicFilterBulderEventsDown);

            connection.SubscribeTopic(listTopic);
            
            return ret;
        }

        /// <summary>
        /// 上报设备消息
        /// 如果需要上报子设备消息，需要调用DeviceMessage的setDeviceId接口设置为子设备的设备id
        /// </summary>
        /// <param name="deviceMessage">设备消息</param>
        public void ReportDeviceMessage(DeviceMessage deviceMessage)
        {
            Report(new PubMessage(string.Format(CommonTopic.TOPIC_MESSAGES_UP, deviceId), JsonUtil.ConvertObjectToJsonString(deviceMessage)));
        }

        /// <summary>
        /// 订阅自定义topic。系统topic由SDK自动订阅，此接口只能用于订阅自定义topic
        /// </summary>
        /// <param name="topic">自定义Topic</param>
        public void SubscribeTopic(string topic)
        {
            try
            {
                List<MqttTopicFilter> listTopic = new List<MqttTopicFilter>();

                var topicFilterBulderPreTopic = new MqttTopicFilterBuilder().WithTopic(string.Format(CommonTopic.PRE_TOPIC, deviceId) + topic).Build();
                listTopic.Add(topicFilterBulderPreTopic);

                connection.SubscribeTopic(listTopic);
                //// rawMessageListenerDic.Add(topic, rawMessageListener);
            }
            catch (Exception ex)
            {
                Log.Error("SDK.Error: Subscribe topic fail, the topic is " + topic);
            }
        }
        
        public void OnMessagePublished(RawMessage message)
        {
            if (messagePublishListener != null)
            {
                messagePublishListener.OnMessagePublished(message);
            }
        }

        public void OnMessageUnPublished(RawMessage message)
        {
            if (messagePublishListener != null)
            {
                messagePublishListener.OnMessageUnPublished(message);
            }
        }

        public void ConnectComplete()
        {
            if (connectListener != null)
            {
                connectListener.ConnectComplete();
            }
        }

        public void ConnectionLost()
        {
        }

        public void ConnectFail()
        {
        }

        /// <summary>
        /// 事件上报
        /// </summary>
        /// <param name="evnt">事件</param>
        public void ReportEvent(DeviceEvent evnt)
        {
            string deviceId = clientConf.DeviceId;
            DeviceEvents events = new DeviceEvents();
            events.deviceId = deviceId;
            List<DeviceEvent> services = new List<DeviceEvent>();
            services.Add(evnt);
            events.services = services;

            Report(new PubMessage(CommonTopic.TOPIC_SYS_EVENTS_UP, events));
        }

        /// <summary>
        /// 上报设备属性
        /// </summary>
        /// <param name="properties">设备属性列表</param>
        public void ReportProperties(List<ServiceProperty> properties)
        {
            Report(new PubMessage(properties));
        }

        /// <summary>
        /// 消息上行
        /// </summary>
        /// <typeparam name="T">实体对象</typeparam>
        /// <param name="pubMessage"></param>
        public void Report<T>(T pubMessage) where T : PubMessage
        {
            try
            {
                RawMessage rawMessage = new RawMessage(string.Format(pubMessage.Topic, deviceId), pubMessage.Message);
                connection.PublishMessage(rawMessage);
            }
            catch (Exception ex)
            {
                Log.Error("SDK.Error: Report msg error, the message is " + pubMessage.Message);
            }
        }

        private void CheckClientConf(ClientConf clientConf)
        {
            if (clientConf == null)
            {
                throw new Exception("clientConf is null");
            }

            if (clientConf.DeviceId == null)
            {
                throw new Exception("clientConf.deviceId is null");
            }

            if (clientConf.ServerUri == null)
            {
                throw new Exception("clientConf.getSecret() is null");
            }
        }

        private void OnEvent(RawMessage message)
        {
            DeviceEvents deviceEvents = JsonUtil.ConvertJsonStringToObject<DeviceEvents>(message.ToString());

            if (deviceEvents == null)
            {
                Log.Error("invalid events");
                return;
            }

            device.OnEvent(deviceEvents);
        }

        private void OnCommandV3(RawMessage message)
        {
            CommandV3 commandV3 = JsonUtil.ConvertJsonStringToObject<CommandV3>(message.ToString());
            if (commandV3 == null)
            {
                Log.Error("invalid commandV3");
                return;
            }

            if (commandV3Listener != null)
            {
                commandV3Listener.OnCommandV3(commandV3);
            }
        }

        private void OnShadowCommand(RawMessage message)
        {
            string requestId = IotUtil.GetRequestId(message.Topic);
            deviceShadowListener.OnShadowCommand(requestId, message.Payload);
        }

        private void OnDeviceMessage(RawMessage message)
        {
            DeviceMessage deviceMessage = new DeviceMessage();
            deviceMessage.content = message.ToString();
            deviceMessageListener.OnDeviceMessage(deviceMessage);
        }

        private void OnPropertiesSet(RawMessage message)
        {
            string requestId = IotUtil.GetRequestId(message.Topic);

            PropsSet propsSet = JsonUtil.ConvertJsonStringToObject<PropsSet>(message.ToString());
            if (propsSet == null)
            {
                return;
            }

            // 只处理直连设备的，子设备的由AbstractGateway处理
            if (propertyListener != null && (propsSet.deviceId == null || propsSet.deviceId == this.deviceId))
            {
                propertyListener.OnPropertiesSet(requestId, propsSet.services);

                return;
            }

            device.OnPropertiesSet(requestId, propsSet);
        }

        private void OnPropertiesGet(RawMessage message)
        {
            string requestId = IotUtil.GetRequestId(message.Topic);

            PropsGet propsGet = JsonUtil.ConvertJsonStringToObject<PropsGet>(message.ToString());
            if (propsGet == null)
            {
                return;
            }

            if (propertyListener != null && (propsGet.deviceId == null || propsGet.deviceId == this.deviceId))
            {
                propertyListener.OnPropertiesGet(requestId, propsGet.serviceId);

                return;
            }

            device.OnPropertiesGet(requestId, propsGet);
        }

        /// <summary>
        /// 自定义Topic，下发设备消息
        /// </summary>
        /// <param name="message"></param>
        private void OnCustomCommand(RawMessage message)
        {
            deviceCustomMessageListener.OnCustomMessageCommand(message.Payload);
        }
    }
}
