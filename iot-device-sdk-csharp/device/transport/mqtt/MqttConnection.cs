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
using System.Text;
using System.Threading;
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Log.Requests;
using IoT.SDK.Device.Utils;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Receiving;
using MQTTnet.Extensions.ManagedClient;
using NLog;

namespace IoT.SDK.Device.Transport.Mqtt
{
    internal class MqttConnection : Connection
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private ManualResetEvent mre = new ManualResetEvent(false);
        private IManagedMqttClient client = null;


        private readonly ClientConf clientConf;

        private HashSet<ConnectListener> ConnectListener { get; } = new HashSet<ConnectListener>();

        private readonly RawMessageListener rawMessageListener;


        public MqttConnection(ClientConf clientConf, RawMessageListener rawMessageListener,
            ConnectListener connectListener)
        {
            this.clientConf = clientConf;
            this.rawMessageListener = rawMessageListener;
            this.ConnectListener.Add(connectListener);
        }

        private void InitMqttClient()
        {
            if (clientConf.AutoReconnect)
            {
                client = new CustomReconnectDelayMqttFactoryProxy(
                    new MqttFactory(), this, new IoTMqttOptionsGetter(clientConf)).CreateManagedMqttClient();
            }
            else
            {
                client = new MqttFactory().CreateManagedMqttClient();
            }

            // Registers events.
            // Callback for message publish.
            client.ApplicationMessageProcessedHandler =
                new ApplicationMessageProcessedHandlerDelegate(ApplicationMessageProcessedHandlerMethod);
            // Callback for command delivered.
            client.ApplicationMessageReceivedHandler =
                new MqttApplicationMessageReceivedHandlerDelegate(MqttApplicationMessageReceived);
            client.ConnectedHandler =
                new MqttClientConnectedHandlerDelegate(OnMqttClientConnected);
            // Callback for disconnected.
            client.DisconnectedHandler =
                new MqttClientDisconnectedHandlerDelegate(OnMqttClientDisconnected);
            client.ConnectingFailedHandler =
                new ConnectingFailedHandlerDelegate(OnMqttClientConnectingFailed);
        }

        /// <summary>
        /// Connects to the platform.
        /// </summary>
        /// <returns></returns>
        public int Connect()
        {
            try
            {
                InitMqttClient();
                LOG.Info("try to connect to server {:l}:{}", clientConf.ServerUri, clientConf.Port);
                IManagedMqttClientOptions options = new IoTMqttOptionsGetter(clientConf).GetManagedOptions();

                // Connects to the platform.
                client.StartAsync(options);
                mre.Reset();

                mre.WaitOne();

                return client.IsConnected ? 0 : -1;
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "SDK.Error: Connect to mqtt server fail, device id:{}", clientConf.DeviceId);
                return -1;
            }
        }

        public void PublishMessage(RawMessage message)
        {
            string topic = message.Topic;
            LOG.Debug("publish message topic = {}", topic);

            if (string.IsNullOrEmpty(topic))
            {
                LOG.Error("publish message is null");
            }

            var appMsg = new MqttApplicationMessage();
            appMsg.Payload = message.BinPayload;
            appMsg.Topic = topic;
            appMsg.QualityOfServiceLevel = clientConf.Qos;
            appMsg.Retain = false;
            if (message.MqttV5Data != null)
            {
                appMsg.CorrelationData = message.MqttV5Data?.CorrelationData;
                appMsg.UserProperties = message.MqttV5Data?.UserProperties;
                appMsg.ContentType = message.MqttV5Data?.ContentType;
                appMsg.ResponseTopic = message.MqttV5Data?.ResponseTopic;
            }

            // Responds to the upstream message.
            var task = client.PublishAsync(appMsg);
            task.Wait();
            var result = task.Result;

            LOG.Debug("publish msg content:{}, reason code:{}, package indentifier:{}",
                message.Payload, result.ReasonCode, result.PacketIdentifier);
        }

        public void Close()
        {
            try
            {
                // client.StopAsync().Wait();
                client.Dispose();
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "SDK.Error: Close connection error, device id:{}", clientConf.DeviceId);
            }
        }

        public bool IsConnected()
        {
            if (client == null)
            {
                return false;
            }

            return client.IsConnected;
        }

        public void AddConnectListener(ConnectListener listener)
        {
            this.ConnectListener.Add(listener);
        }

        public void SubscribeTopic(List<MqttTopicFilter> listTopic)
        {
            try
            {
                if (!client.IsConnected)
                {
                    LOG.Debug("The MQTT client is not connected.");
                    return;
                }

                if (listTopic.Count <= 0)
                {
                    LOG.Debug("The topic list cannot be empty.");
                    return;
                }

                // Subscribes to a topic.
                client.SubscribeAsync(listTopic.ToArray()).Wait();
                LOG.Debug("succeed to subscribe topics");
                foreach (MqttTopicFilter mqttTopicFilter in listTopic)
                {
                    LOG.Debug("succeed topic: {}", mqttTopicFilter.Topic);
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "topic subscribe failed");
                foreach (var mqttTopicFilter in listTopic)
                {
                    LOG.Debug("failed topic: {}", mqttTopicFilter.Topic);
                }
            }
        }

        /// <summary>
        /// Receives a message.
        /// </summary>
        /// <param name="e"></param>
        private void MqttApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            string payload = null;
            if (e.ApplicationMessage.Payload != null)
            {
                payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            }

            LOG.Info("messageArrived, topic: {}, msg: {}", e.ApplicationMessage.Topic, payload);
            RawMessage rawMessage = new RawMessage(e.ApplicationMessage.Topic, payload);
            if (e.ApplicationMessage.UserProperties != null || e.ApplicationMessage.ContentType != null
                                                            || e.ApplicationMessage.ResponseTopic != null
                                                            || e.ApplicationMessage.CorrelationData != null)
            {
                rawMessage.MqttV5Data = new MqttV5Data
                {
                    UserProperties = e.ApplicationMessage.UserProperties,
                    ContentType = e.ApplicationMessage.ContentType,
                    ResponseTopic = e.ApplicationMessage.ResponseTopic,
                    CorrelationData = e.ApplicationMessage.CorrelationData,
                };
            }

            try
            {
                rawMessageListener?.OnMessageReceived(rawMessage);
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "SDK.Error: Message received error, message:{}", payload);
            }
        }

        private void OnMqttClientConnected(MqttClientConnectedEventArgs e)
        {
            LOG.Info("connect success, device id:{}", clientConf.DeviceId);
            foreach (var connectListener in ConnectListener)
            {
                connectListener.ConnectComplete();
            }

            mre.Set();
        }

        private void OnMqttClientConnectingFailed(ManagedProcessFailedEventArgs e)
        {
            try
            {
                LOG.Error(e.Exception, "SDK.Error: Connect fail, device id:{}", clientConf.DeviceId);

                foreach (var connectListener in ConnectListener)
                {
                    connectListener.ConnectFail();
                }

                // If the password is wrong, do not reconnect.
                if (e.Exception.Message.Contains("BadUserNameOrPassword"))
                {
                    LOG.Info("bad user name or password");
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "SDK.Error: ConnectFail callback fail, device id:{}", clientConf.DeviceId);
            }
        }

        /// <summary>
        /// Disconnects from the platform.
        /// </summary>
        /// <param name="e"></param>
        private void OnMqttClientDisconnected(MqttClientDisconnectedEventArgs e)
        {
            LOG.Error(e.Exception, "Connect lost, device id:{}", clientConf.DeviceId);
            foreach (var connectListener in ConnectListener)
            {
                connectListener.ConnectionLost();
            }
        }

        /// <summary>
        /// Callback for message publish.
        /// </summary>
        /// <param name="e"></param>
        private void ApplicationMessageProcessedHandlerMethod(ApplicationMessageProcessedEventArgs e)
        {
            try
            {
                if (rawMessageListener == null)
                {
                    LOG.Error("rawMessageListener is null");
                    return;
                }

                var stringPayload = Encoding.UTF8.GetString(e.ApplicationMessage.ApplicationMessage.Payload);
                var messageId = e.ApplicationMessage.Id.ToString();
                var topic = e.ApplicationMessage.ApplicationMessage.Topic;
                RawMessage rawMessage = new RawMessage(messageId, topic, stringPayload);

                if (e.HasFailed)
                {
                    LOG.Info("publish failed, messageId: {0}, topic: {1}, payload: {2}",
                        messageId, topic, stringPayload);

                    rawMessageListener.OnMessageUnPublished(rawMessage);
                }
                else if (e.HasSucceeded)
                {
                    LOG.Debug("publish succeed messageId: {0}, topic: {1}, payload: {2}",
                        messageId, topic, stringPayload);

                    rawMessageListener.OnMessagePublished(rawMessage);
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "SDK.Error: message being published:{0} ",
                    Encoding.UTF8.GetString(e.ApplicationMessage.ApplicationMessage.Payload));
            }
        }

        /// <summary>
        /// Save the latest error information which is connection failure and disconnection.
        /// </summary>
        /// <param name="logType">Indicates the log type</param>
        /// <param name="content">Indicates the log content</param>
        private void SaveLog(string logType, string content)
        {
            try
            {
                Dictionary<string, LogInfo> logDic =
                    JsonUtil.ConvertJsonStringToDic<string, LogInfo>(IotUtil.ReadJsonFile(CommonFilePath.LOG_PATH));

                if (logDic.ContainsKey(logType))
                {
                    logDic[logType].content = content;
                    logDic[logType].eventTime = IotUtil.GetEventTime();
                }
                else
                {
                    LogInfo li = new LogInfo();
                    li.content = content;
                    li.eventTime = IotUtil.GetEventTime();
                    logDic.Add(logType, li);
                }

                IotUtil.WriteJsonFile(CommonFilePath.LOG_PATH, JsonUtil.ConvertObjectToJsonString(logDic));
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "SDK.Error: SaveLog failed");
            }
        }
    }
}