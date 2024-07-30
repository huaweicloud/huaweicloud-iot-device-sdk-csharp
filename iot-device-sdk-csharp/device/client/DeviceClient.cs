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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Service;
using IoT.SDK.Device.Service.DeviceRule;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Transport.Mqtt;
using IoT.SDK.Device.Utils;
using MQTTnet;
using Newtonsoft.Json;
using NLog;

namespace IoT.SDK.Device.Client
{
    public class DeviceClient : RawMessageListener, ConnectListener
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private const double AUTO_REPORT_DEVICE_INFO_TIME = 5;

        private const string SDK_VERSION = "C#_v1.3.4";

        private CommandV3Listener commandV3Listener;

        private readonly ClientConf clientConf;

        public Connection Connection { get; set; }

        private readonly string deviceId;

        private readonly Dictionary<string, RawMessageListener> rawMessageListenerDic;

        private readonly HashSet<string> customTopics;

        private readonly AbstractDevice device;

        private Timer timer;


        public DeviceClient(ClientConf clientConf, AbstractDevice device)
        {
            this.CheckClientConf(clientConf);
            this.clientConf = clientConf;
            this.deviceId = clientConf.DeviceId;
            this.Connection = new MqttConnection(clientConf, this, this);
            this.device = device;
            this.rawMessageListenerDic = new Dictionary<string, RawMessageListener>();
            customTopics = new HashSet<string>();
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

                if (customTopics.Contains(topic))
                {
                    rawDeviceMessageListener?.OnCustomRawDeviceMessage(topic, false,
                        new RawDeviceMessage(message.BinPayload));
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
                    rawDeviceMessageListener?.OnCustomRawDeviceMessage(topic, true,
                        new RawDeviceMessage(message.BinPayload));
                    OnCustomCommand(message);
                }
                else
                {
                    LOG.Error("unknown topic:{}", topic);
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "SDK.Error: message received error, the topic is {}", topic);
            }
        }

        /// <summary>
        /// Reports a command response.
        /// </summary>
        /// <param name="requestId">Indicates the request ID, which must be the same as that in the request.</param>
        /// <param name="commandRsp">Indicates the command response to report.</param>
        public void RespondCommand(string requestId, CommandRsp commandRsp)
        {
            if (requestId == DeviceRuleService.ServiceId)
            {
                return;
            }

            Report(new PubMessage(CommonTopic.TOPIC_COMMANDS_RESPONSE + "=" + requestId,
                JsonUtil.ConvertObjectToJsonString(commandRsp)));
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
            Report(new PubMessage(CommonTopic.TOPIC_SYS_PROPERTIES_SET_RESPONSE + "=" + requestId,
                JsonUtil.ConvertObjectToJsonString(iotResult)));
        }

        public void OnCommand(RawMessage message)
        {
            string requestId = IotUtil.GetRequestId(message.Topic);

            Command command = JsonUtil.ConvertJsonStringToObject<Command>(message.ToString());

            OnCommand(requestId, command);
        }

        public void OnCommand(string requestId, Command command)
        {
            if (command == null)
            {
                LOG.Error("invalid command");
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
            int ret = Connection.Connect();

            var listTopic = new List<MqttTopicFilter>();

            var topicFilterBuilderMsgDown = new MqttTopicFilterBuilder()
                .WithTopic(string.Format(CommonTopic.TOPIC_SYS_MESSAGES_DOWN, deviceId)).Build();
            listTopic.Add(topicFilterBuilderMsgDown);

            var topicFilterBuilderCommand = new MqttTopicFilterBuilder()
                .WithTopic(string.Format(CommonTopic.TOPIC_SYS_COMMAND, deviceId)).Build();
            listTopic.Add(topicFilterBuilderCommand);

            var topicFilterBuilderShadowGetResponse = new MqttTopicFilterBuilder()
                .WithTopic(string.Format(CommonTopic.TOPIC_SYS_SHADOW_GET_RESPONSE, deviceId)).Build();
            listTopic.Add(topicFilterBuilderShadowGetResponse);

            var topicFilterBuilderPropertiesSet = new MqttTopicFilterBuilder()
                .WithTopic(string.Format(CommonTopic.TOPIC_SYS_PROPERTIES_SET, deviceId)).Build();
            listTopic.Add(topicFilterBuilderPropertiesSet);

            var topicFilterBuilderPropertiesGet = new MqttTopicFilterBuilder()
                .WithTopic(string.Format(CommonTopic.TOPIC_SYS_PROPERTIES_GET, deviceId)).Build();
            listTopic.Add(topicFilterBuilderPropertiesGet);

            var topicFilterBuilderEventsDown = new MqttTopicFilterBuilder()
                .WithTopic(string.Format(CommonTopic.TOPIC_SYS_EVENTS_DOWN, deviceId)).Build();
            listTopic.Add(topicFilterBuilderEventsDown);

            Connection.SubscribeTopic(listTopic);

            return ret;
        }

        /// <summary>
        /// Reports a device message.
        /// To report a message for a child device, call the setDeviceId API of DeviceMessage to set the device ID of the child device.
        /// </summary>
        /// <param name="deviceMessage">Indicates the device message to report.</param>
        public void ReportDeviceMessage(DeviceMessage deviceMessage)
        {
            Report(new PubMessage(string.Format(CommonTopic.TOPIC_MESSAGES_UP, deviceId),
                JsonUtil.ConvertObjectToJsonString(deviceMessage))
            {
                MqttV5Data = deviceMessage.MqttV5Data
            });
        }

        /// <summary>
        /// Reports a raw device message.
        /// To report a message for a child device, call the setDeviceId API of DeviceMessage to set the device ID of the child device.
        /// </summary>
        /// <param name="deviceMessage">Indicates the device message to report.</param>
        /// <param name="customMessageTopic"></param>
        public void ReportRawDeviceMessage(RawDeviceMessage deviceMessage, CustomMessageTopic customMessageTopic = null)
        {
            var topic = string.Format(CommonTopic.TOPIC_MESSAGES_UP, deviceId);
            if (customMessageTopic != null)
            {
                topic = GetFullTopicFromCustomTopic(customMessageTopic);
            }

            Report(new PubMessage(topic, deviceMessage.payload)
            {
                MqttV5Data = deviceMessage.MqttV5Data
            });
        }

        /// <summary>
        /// Subscribes to a custom topic. System topics are automatically subscribed by the SDK. This method can be used only to subscribe to custom topics.
        /// </summary>
        /// <param name="topic">Indicates the name of the custom topic.</param>
        public string SubscribeTopic(string topic)
        {
            try
            {
                List<MqttTopicFilter> listTopic = new List<MqttTopicFilter>();

                var topicFilterBuilderPreTopic = new MqttTopicFilterBuilder()
                    .WithTopic(string.Format(CommonTopic.PRE_TOPIC, deviceId) + topic).Build();
                listTopic.Add(topicFilterBuilderPreTopic);

                Connection.SubscribeTopic(listTopic);
                return topicFilterBuilderPreTopic.Topic;
                //// rawMessageListenerDic.Add(topic, rawMessageListener);
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "SDK.Error: Subscribe topic fail, topic:{}", topic);
                return null;
            }
        }

        public string SubscribeTopic(CustomMessageTopic topic)
        {
            try
            {
                List<MqttTopicFilter> listTopic = new List<MqttTopicFilter>();

                var topicFilterBuilderPreTopic = new MqttTopicFilterBuilder()
                    .WithTopic(GetFullTopicFromCustomTopic(topic)).Build();
                listTopic.Add(topicFilterBuilderPreTopic);

                Connection.SubscribeTopic(listTopic);
                var t = topicFilterBuilderPreTopic.Topic;
                if (!topic.OcPrefix && rawMessageListenerDic.TryGetValue(t, out var existingListener) &&
                    existingListener == null)
                {
                    rawMessageListenerDic.Add(t, null);
                }

                return topicFilterBuilderPreTopic.Topic;
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "SDK.Error: Subscribe topic fail, topic:{}", topic);
                return null;
            }
        }

        private string GetFullTopicFromCustomTopic(CustomMessageTopic topic)
        {
            var topicString = topic.Suffix;
            if (topic.OcPrefix)
            {
                topicString = string.Format(CommonTopic.PRE_TOPIC, deviceId) + topicString;
            }

            return topicString;
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

                var topicFilterBuilderPreTopic = new MqttTopicFilterBuilder().WithTopic(topic).Build();
                listTopic.Add(topicFilterBuilderPreTopic);

                Connection.SubscribeTopic(listTopic);
                customTopics.Add(topic);
                if (topicKey == null || listener == null) return;
                rawMessageListenerDic.TryAdd(topicKey, listener);
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "SDK.Error: Subscribe topic fail, topic:{} ", topic);
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
            timer = new Timer(p => ReportSdkInfoSync("v1.0", "v1.0"), autoEvent, TimeSpan.FromSeconds(0),
                TimeSpan.FromDays(AUTO_REPORT_DEVICE_INFO_TIME));


            connectListener?.ConnectComplete();
        }

        public void ConnectionLost()
        {
            connectListener?.ConnectionLost();
            timer?.Dispose();
        }

        public void ConnectFail()
        {
            connectListener?.ConnectFail();
        }

        public void ReportSdkInfoSync(string swVersion, string fwVersion)
        {
            string sdkInfoStr = IotUtil.ReadJsonFile(CommonFilePath.DEVICE_INFO_PATH);
            try
            {
                if (!string.IsNullOrEmpty(sdkInfoStr))
                {
                    Dictionary<string, string> sdkInfoDic = JsonUtil.ConvertJsonStringToDic<string, string>(sdkInfoStr);

                    if (sdkInfoDic["sw_version"]?.ToString() == swVersion &&
                        sdkInfoDic["fw_version"]?.ToString() == fwVersion)
                    {
                        DateTime start =
                            Convert.ToDateTime(DateTime.Parse(sdkInfoDic["event_time"]?.ToString())
                                .ToShortDateString());
                        DateTime end = DateTime.Now;
                        TimeSpan sp = end.Subtract(start);

                        if (sp.Days < AUTO_REPORT_DEVICE_INFO_TIME)
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LOG.Error(e, "check last device info failed");
            }

            var node = GenerateDeviceInfo(swVersion, fwVersion);
            IotUtil.WriteJsonFile(CommonFilePath.DEVICE_INFO_PATH, JsonUtil.ConvertObjectToJsonString(node));
            ReportDeviceInfo(swVersion, fwVersion);
        }

        public void ReportDeviceInfo(string swVersion, string fwVersion)
        {
            var node = GenerateDeviceInfo(swVersion, fwVersion);

            var deviceEvent = new DeviceEvent
            {
                serviceId = "$sdk_info",
                eventType = "sdk_info_report",
                paras = node,
                eventTime = IotUtil.GetEventTime()
            };

            ReportEvent(deviceEvent);
        }

        private Dictionary<string, object> GenerateDeviceInfo(string swVersion, string fwVersion)
        {
            return new Dictionary<string, object>
            {
                { "device_sdk_version", SDK_VERSION },
                { "sw_version", swVersion },
                { "fw_version", fwVersion },
                { "event_time", DateTime.Now.ToString(CultureInfo.InvariantCulture) }
            };
        }


        /// <summary>
        /// Reports an event.
        /// </summary>
        /// <param name="evnt">Indicates the event to report.</param>
        public void ReportEvent(DeviceEvent evnt)
        {
            ReportEvent(null, evnt);
        }

        public void ReportEvent(DeviceEvents deviceEvents)
        {
            Report(new PubMessage(CommonTopic.TOPIC_SYS_EVENTS_UP, deviceEvents));
        }

        public void ReportEvent(string objectDeviceId, DeviceEvent deviceEvent)
        {
            var services = new List<DeviceEvent> { deviceEvent };
            var events = new DeviceEvents
            {
                deviceId = objectDeviceId ?? clientConf.DeviceId,
                services = services
            };
            ReportEvent(events);
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
        /// Reports device properties.
        /// </summary>
        /// <param name="properties">Indicates the properties to report.</param>
        public void ReportProperties(DeviceProperties properties)
        {
            Report(new PubMessage(CommonTopic.TOPIC_PROPERTIES_REPORT, JsonConvert.SerializeObject(properties)));
        }

        public string GetShadow(DeviceShadowRequest shadowRequest, string requestId = null)
        {
            requestId ??= Guid.NewGuid().ToString();
            var topic = CommonTopic.TOPIC_SYS_SHADOW_GET + "=" + requestId;
            var msg = JsonConvert.SerializeObject(shadowRequest);
            Report(new PubMessage(topic, msg));
            return requestId;
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
                var rawMessage = new RawMessage(string.Format(pubMessage.Topic, deviceId), pubMessage.Payload);
                Connection.PublishMessage(rawMessage);
                device.CatchPropertiesForDeviceRuleProperties(pubMessage);
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "SDK.Error: Report msg failed");
                LOG.Error("The report message is {}", pubMessage.Message);
            }
        }

        /// <summary>
        /// Close connection
        /// </summary>
        public void Close()
        {
            Connection.Close();
        }

        public bool IsConnected()
        {
            return Connection.IsConnected();
        }

        private void CheckClientConf(ClientConf config)
        {
            if (config == null)
            {
                throw new Exception("clientConf is null");
            }

            if (config.DeviceId == null)
            {
                throw new Exception("clientConf.deviceId is null");
            }

            if (config.ServerUri == null)
            {
                throw new Exception("clientConf ServerAddress is null");
            }
        }

        private void OnEvent(RawMessage message)
        {
            DeviceEvents deviceEvents = JsonUtil.ConvertJsonStringToObject<DeviceEvents>(message.ToString());

            if (deviceEvents == null)
            {
                LOG.Error("invalid events");
                return;
            }

            device.OnEvent(deviceEvents);
        }

        private void OnCommandV3(RawMessage message)
        {
            CommandV3 commandV3 = JsonUtil.ConvertJsonStringToObject<CommandV3>(message.ToString());
            if (commandV3 == null)
            {
                LOG.Error("invalid commandV3");

                return;
            }

            if (commandV3Listener != null)
            {
                commandV3Listener.OnCommandV3(commandV3);
            }
        }

        private void OnShadowCommand(RawMessage message)
        {
            var requestId = IotUtil.GetRequestId(message.Topic);
            deviceShadowListener?.OnShadowCommand(requestId, message.Payload);

            var shadows = JsonUtil.ConvertJsonStringToObject<DeviceShadowResponse>(message.Payload);
            device.OnDeviceShadow(requestId, shadows);
        }

        private void OnDeviceMessage(RawMessage message)
        {
            var rawDeviceMessage = new RawDeviceMessage(message.BinPayload)
            {
                MqttV5Data = message.MqttV5Data
            };
            rawDeviceMessageListener?.OnRawDeviceMessage(rawDeviceMessage);


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

            device.OnDeviceMessage(deviceMessage);
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
            deviceCustomMessageListener?.OnCustomMessageCommand(message.Payload);
        }
    }
}