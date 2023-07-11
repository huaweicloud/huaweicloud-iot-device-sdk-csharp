/*
 * Copyright (c) 2020-2023 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Threading;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Service;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Transport.Mqtt;
using IoT.SDK.Device.Utils;
using MQTTnet;
using NLog;

namespace IoT.SDK.Device.Client
{
    public class DeviceClient : RawMessageListener, ConnectListener
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly double AUTO_REPORT_DEVICE_INFO_TIME = 5;

        private static readonly string SDK_VERSION = "C#_v1.3.1";

        private CommandV3Listener commandV3Listener;

        private ClientConf clientConf;

        private Connection connection;

        private string deviceId;

        private Dictionary<string, RawMessageListener> rawMessageListenerDic;

        private AbstractDevice device;

        private Timer timer;
        
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

        public RawDeviceMessageListener rawDeviceMessageListener { get; set; }

        public DeviceMessageListener deviceMessageListener { get; set; }

        public ConnectListener connectListener { get; set; }

        public BootstrapMessageListener bootstrapMessageListener { get; set; }

        public virtual void OnMessageReceived(RawMessage message)
        {
            string topic = message.Topic;
            try
            {
                RawMessageListener listener = null;
                foreach (var listenItem in rawMessageListenerDic)
                {
                    if (topic.Contains(listenItem.Key))
                    {
                        listener = listenItem.Value;
                        listener.OnMessageReceived(message);
                        return;
                    }
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
        /// Reports a command response.
        /// </summary>
        /// <param name="requestId">Indicates the request ID, which must be the same as that in the request.</param>
        /// <param name="commandRsp">Indicates the command response to report.</param>
        public void RespondCommand(string requestId, CommandRsp commandRsp)
        {
            Report(new PubMessage(CommonTopic.TOPIC_COMMANDS_RESPONSE + "=" + requestId, JsonUtil.ConvertObjectToJsonString(commandRsp)));
        }

        /// <summary>
        /// Reports a response to a property query request.
        /// </summary>
        /// <param name="requestId">Indicates the request ID, which must be the same as that in the request.</param>
        /// <param name="services">Indicates service properties.</param>
        public void RespondPropsGet(string requestId, List<ServiceProperty> services)
        {
            DeviceProperties deviceProperties = new DeviceProperties();
            deviceProperties.services = services;

            Report(new PubMessage(CommonTopic.TOPIC_SYS_PROPERTIES_GET_RESPONSE + "=" + requestId, deviceProperties));
        }

        /// <summary>
        /// Reports a response to a property setting request.
        /// </summary>
        /// <param name="requestId">Indicates the request ID, which must be the same as that in the request.</param>
        /// <param name="iotResult">Indicates the property setting result.</param>
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
        /// Connects to the platform. This method blocks the calling thread and the timeout duration is 20 seconds. When the connection is established, the SDK automatically subscribes to system topics.
        /// </summary>
        /// <returns>Returns 0 if the connection is successful; returns other values if the connection fails.</returns>
        public int Connect()
        {
            int ret = connection.Connect();
            
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
        /// Reports a device message.
        /// To report a message for a child device, call the setDeviceId API of DeviceMessage to set the device ID of the child device.
        /// </summary>
        /// <param name="deviceMessage">Indicates the device message to report.</param>
        public void ReportDeviceMessage(DeviceMessage deviceMessage)
        {
            Report(new PubMessage(string.Format(CommonTopic.TOPIC_MESSAGES_UP, deviceId), JsonUtil.ConvertObjectToJsonString(deviceMessage)));
        }

        /// <summary>
        /// Subscribes to a custom topic. System topics are automatically subscribed by the SDK. This method can be used only to subscribe to custom topics.
        /// </summary>
        /// <param name="topic">Indicates the name of the custom topic.</param>
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

        /// <summary>
        /// Subscribes to a Complete topic. This method can be used only to subscribe to custom topics.
        /// </summary>
        /// <param name="topic">Indicates the name of the custom topic.</param>
        /// <param name="topicKey">Indicates the key message of the custom topic.</param>
        /// <param name="listener">Indicates the listener of the topicKey.</param>
        public void SubscribeCompleteTopic(string topic, string topicKey, RawMessageListener listener)
        {
            try
            {
                List<MqttTopicFilter> listTopic = new List<MqttTopicFilter>();

                var topicFilterBulderPreTopic = new MqttTopicFilterBuilder().WithTopic(topic).Build();
                listTopic.Add(topicFilterBulderPreTopic);

                connection.SubscribeTopic(listTopic);
                if (topicKey == null || listener == null) return;
                if (!rawMessageListenerDic.ContainsKey(topicKey))
                {
                    rawMessageListenerDic.Add(topicKey, listener);
                }
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
            var autoEvent = new AutoResetEvent(true);
            timer = new Timer(p => ReportSdkInfoSync("v1.0", "v1.0"), autoEvent, TimeSpan.FromSeconds(0), TimeSpan.FromDays(AUTO_REPORT_DEVICE_INFO_TIME));

            if (connectListener != null)
            {
                connectListener.ConnectComplete();
            }
        }

        public void ConnectionLost()
        {
            if (timer != null)
            {
                timer.Dispose();
            }
        }

        public void ConnectFail()
        {
        }

        public void ReportSdkInfoSync(string swVersion, string fwVersion)
        {
            string sdkInfoStr = IotUtil.ReadJsonFile(CommonFilePath.DEVICE_INFO_PATH);

            if (!string.IsNullOrEmpty(sdkInfoStr))
            {
                Dictionary<string, string> sdkInfoDic = JsonUtil.ConvertJsonStringToDic<string, string>(sdkInfoStr);

                if (sdkInfoDic["sw_version"]?.ToString() == swVersion && sdkInfoDic["fw_version"]?.ToString() == fwVersion)
                {
                    DateTime start = Convert.ToDateTime(DateTime.Parse(sdkInfoDic["event_time"]?.ToString()).ToShortDateString());
                    DateTime end = DateTime.Now;
                    TimeSpan sp = end.Subtract(start);
                    
                    if (sp.Days < AUTO_REPORT_DEVICE_INFO_TIME)
                    {
                        return;
                    }
                }
            }
            
            Dictionary<string, object> node = new Dictionary<string, object>();
            node.Add("device_sdk_version", SDK_VERSION);
            node.Add("sw_version", swVersion);
            node.Add("fw_version", fwVersion);

            DeviceEvent deviceEvent = new DeviceEvent();
            deviceEvent.eventType = "sdk_info_report";
            deviceEvent.paras = node;
            deviceEvent.serviceId = "$sdk_info";
            deviceEvent.eventTime = IotUtil.GetEventTime();

            node.Add("event_time", DateTime.Now.ToString());
            IotUtil.WriteJsonFile(CommonFilePath.DEVICE_INFO_PATH, JsonUtil.ConvertObjectToJsonString(node));

            ReportEvent(deviceEvent);
        }

        /// <summary>
        /// Reports an event.
        /// </summary>
        /// <param name="evnt">Indicates the event to report.</param>
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
        /// Reports device properties.
        /// </summary>
        /// <param name="properties">Indicates the properties to report.</param>
        public void ReportProperties(List<ServiceProperty> properties)
        {
            Report(new PubMessage(properties));
        }

        /// <summary>
        /// Reports a message.
        /// </summary>
        /// <typeparam name="T">Indicates a physical object.</typeparam>
        /// <param name="pubMessage"></param>
        public virtual void Report<T>(T pubMessage) where T : PubMessage
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

        /// <summary>
        /// Close connection
        /// </summary>
        public void Close()
        {
            connection.Close();
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
            RawDeviceMessage rawDeviceMessage = new RawDeviceMessage(message.BinPayload);
            if (rawDeviceMessageListener != null)
            {
                rawDeviceMessageListener.OnRawDeviceMessage(rawDeviceMessage);
            }


            DeviceMessage deviceMessage = rawDeviceMessage.ToDeviceMessage();
            if (deviceMessage == null)
            {
                return; // isn't system format
            }

            if (deviceMessage.deviceId == null || deviceMessage.deviceId == deviceId)
            {
                if (deviceMessageListener != null)
                {
                    deviceMessageListener.OnDeviceMessage(deviceMessage);
                    return;
                }

                HandleDeviceMessage(deviceMessage);
            }
        }

        /// <summary>
        /// Processes a message delivered by the platform.
        /// </summary>
        /// <param name="deviceMessage">deviceMessage Indicates the device message delivered.</param>
        private void HandleDeviceMessage(DeviceMessage deviceMessage)
        {
            // Add the device method rebooting function. If the message content is BootstrapRequestTrigger, the device needs to initiate rebooting.
            if (deviceMessage.content == "BootstrapRequestTrigger" && bootstrapMessageListener != null)
            {
                bootstrapMessageListener.OnRetryBootstrapMessage();
            }
        }
        
        private void OnPropertiesSet(RawMessage message)
        {
            string requestId = IotUtil.GetRequestId(message.Topic);

            PropsSet propsSet = JsonUtil.ConvertJsonStringToObject<PropsSet>(message.ToString());
            if (propsSet == null)
            {
                return;
            }

            // Only messages delivered to directly connected devices are processed. Messages delivered to child devices are processed by AbstractGateway.
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
        /// Called when a device message from a custom topic is received.
        /// </summary>
        /// <param name="message"></param>
        private void OnCustomCommand(RawMessage message)
        {
            deviceCustomMessageListener.OnCustomMessageCommand(message.Payload);
        }

		public virtual void ReportEvent(string deviceId, DeviceEvent evnt)
        {
            return;
        }
    }
}
