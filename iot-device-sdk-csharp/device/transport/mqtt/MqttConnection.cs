/*
 * Copyright (c) 2020-2022 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Log;
using IoT.SDK.Device.Log.Requests;
using IoT.SDK.Device.Utils;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using NLog;

namespace IoT.SDK.Device.Transport.Mqtt
{
    internal class MqttConnection : Connection
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly ushort DEFAULT_KEEPLIVE = 120;

        private static readonly double RECONNECT_TIME = 10;

        private static readonly double DEFAULT_CONNECT_TIMEOUT = 1;
        
        private static readonly string ConnectType = "0";

        private static readonly string CheckTimestamp = "0";

        private static ManualResetEvent mre = new ManualResetEvent(false);

        private static IManagedMqttClient client = null;

        private static int retryTimes = 0;

        private bool lessThanMaxBackoffTimeFlag = false;

        private long minBackoff = 1000;

        private long maxBackoff = 4 * 60 * 1000; // 4 minutes

        private long defaultBackoff = 1000;

        private Mutex mutex = new Mutex(false);

        private ClientConf clientConf;

        private ConnectListener connectListener;

        private RawMessageListener rawMessageListener;
        
        private Random random = new Random();

        public readonly int MQTTS_PORT = 8883;

        public readonly int MQTT_PORT = 1883;

        public MqttConnection(ClientConf clientConf, RawMessageListener rawMessageListener, ConnectListener connectListener)
        {
            this.clientConf = clientConf;
            this.rawMessageListener = rawMessageListener;
            this.connectListener = connectListener;
        }

        /// <summary>
        /// Connects to the platform.
        /// </summary>
        /// <returns></returns>
        public int Connect()
        {
            try
            {
                client = new MqttFactory().CreateManagedMqttClient();
                
                // Registers events.
                client.ApplicationMessageProcessedHandler = new ApplicationMessageProcessedHandlerDelegate(new Action<ApplicationMessageProcessedEventArgs>(ApplicationMessageProcessedHandlerMethod)); // Callback for message publish.

                client.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(new Action<MqttApplicationMessageReceivedEventArgs>(MqttApplicationMessageReceived)); // Callback for command delivered.

                client.ConnectedHandler = new MqttClientConnectedHandlerDelegate(new Action<MqttClientConnectedEventArgs>(OnMqttClientConnected));

                client.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(new Action<MqttClientDisconnectedEventArgs>(OnMqttClientDisconnected)); // Callback for disconnected.

                client.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(new Action<ManagedProcessFailedEventArgs>(OnMqttClientConnectingFailed));

                Log.Info("try to connect to server " + clientConf.ServerUri);

                IManagedMqttClientOptions options = GetOptions();

                // Connects to the platform.
                client.StartAsync(options);

                mre.Reset();

                mre.WaitOne();

                return client.IsConnected ? 0 : -1;
            }
            catch (Exception ex)
            {
                Log.Error("SDK.Error: Connect to mqtt server fail, the deviceid is " + clientConf.DeviceId);

                return -1;
            }
        }

        public void PublishMessage(RawMessage message)
        {
            string topic = message.Topic;

            Log.Debug("publish message topic = " + topic);

            if (string.IsNullOrEmpty(topic))
            {
                Log.Error("publish message is null");

                return;
            }
            
            var appMsg = new MqttApplicationMessage();
            appMsg.Payload = Encoding.UTF8.GetBytes(message.Payload);
            appMsg.Topic = topic;
            appMsg.QualityOfServiceLevel = clientConf.Qos;
            appMsg.Retain = false;

            // Responds to the upstream message.
            client.PublishAsync(appMsg).Wait();

            Log.Debug($"publish msg : publishing message is " + message.Payload);
        }

        public void Close()
        {
            if (client.IsConnected)
            {
                try
                {
                    // client.StopAsync().Wait();
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error("SDK.Error: Close connection error, deviceid is " + clientConf.DeviceId);
                }
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

        public void SetConnectListener(ConnectListener connectListener)
        {
            this.connectListener = connectListener;
        }

        public void SubscribeTopic(List<MqttTopicFilter> listTopic)
        {
            try
            {
                if (!client.IsConnected)
                {
                    Log.Debug("The MQTT client is not connected.");
                    return;
                }
                
                if (listTopic.Count <= 0)
                {
                    Log.Debug("The topic cannot be left blank.");
                    return;
                }

                // Subscribes to a topic.
                client.SubscribeAsync(listTopic.ToArray()).Wait();

                foreach (MqttTopicFilter mqttTopicFilter in listTopic)
                {
                    Log.Debug($"topic : [{mqttTopicFilter.Topic}] is subscribed success");
                }
            }
            catch (Exception ex)
            {
                foreach (MqttTopicFilter mqttTopicFilter in listTopic)
                {
                    Log.Debug($"topic : [{mqttTopicFilter.Topic}] is subscribed fail");
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

            Log.Info($"messageArrived topic = {e.ApplicationMessage.Topic}, msg = {payload}");
            RawMessage rawMessage = new RawMessage(e.ApplicationMessage.Topic, payload);
            try
            {
                if (rawMessageListener != null)
                {
                    rawMessageListener.OnMessageReceived(rawMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"SDK.Error: Message received error, the message is {payload}");
            }
        }

        private void OnMqttClientConnected(MqttClientConnectedEventArgs e)
        {
            retryTimes = 0;

            Log.Info("connect success, deviceid is " + clientConf.DeviceId);
            if (connectListener != null)
            {
                connectListener.ConnectComplete();
            }

            SaveLog(LogService.MQTT_CONNECTION_SUCCESS, "connect success");
            
            mre.Set();
        }

        private void OnMqttClientConnectingFailed(ManagedProcessFailedEventArgs e)
        {
            try
            {
                if (retryTimes == 0)
                {
                    lessThanMaxBackoffTimeFlag = true;
                }

                Log.Error("SDK.Error: Connect fail, deviceid is " + clientConf.DeviceId);
                if (connectListener != null)
                {
                    connectListener.ConnectFail();
                }

                long waitTimeUtilNextRetry = 0;

                // If the password is wrong, do not reconnect.
                if (e.Exception.Message.Contains("BadUserNameOrPassword"))
                {
                    Log.Info("bad user name or password");
                    return;
                }

                // Backoff reconnection
                Log.Info("reconnect is starting");
                if (lessThanMaxBackoffTimeFlag)
                {
                    if (retryTimes > 0)
                    {
                        int lowBound = (int)(defaultBackoff * 0.8);
                        int highBound = (int)(defaultBackoff * 1.2);
                        long randomBackOff = random.Next(highBound - lowBound);
                        long backOffWithJitter = (int)Math.Pow(2.0, retryTimes - 1) * (randomBackOff + lowBound);
                        waitTimeUtilNextRetry = minBackoff + backOffWithJitter;
                    }

                    if (waitTimeUtilNextRetry < maxBackoff)
                    {
                        retryTimes++;
                    }
                    else
                    {
                        waitTimeUtilNextRetry = maxBackoff;
                        lessThanMaxBackoffTimeFlag = false;
                    }
                }
                else
                {
                    waitTimeUtilNextRetry = maxBackoff;
                }

                Log.Info("connect after time: " + waitTimeUtilNextRetry);

                client.StopAsync();
                client = new MqttFactory().CreateManagedMqttClient();

                IManagedMqttClientOptions options = GetOptions();

                // Registers events.
                client.ApplicationMessageProcessedHandler = new ApplicationMessageProcessedHandlerDelegate(
                    new Action<ApplicationMessageProcessedEventArgs>(
                        ApplicationMessageProcessedHandlerMethod)); // Callback for message publish.

                client.ApplicationMessageReceivedHandler =
                    new MqttApplicationMessageReceivedHandlerDelegate(
                        new Action<MqttApplicationMessageReceivedEventArgs>(
                            MqttApplicationMessageReceived)); // Callback for command delivered.

                client.ConnectedHandler =
                    new MqttClientConnectedHandlerDelegate(
                        new Action<MqttClientConnectedEventArgs>(OnMqttClientConnected));

                client.ConnectingFailedHandler =
                    new ConnectingFailedHandlerDelegate(
                        new Action<ManagedProcessFailedEventArgs>(OnMqttClientConnectingFailed));

                Thread.Sleep((int)waitTimeUtilNextRetry);
                client.StartAsync(options);
            }
            catch (Exception ex)
            {
                Log.Error("SDK.Error: Connect fail, deviceid is " + clientConf.DeviceId);
            }

            SaveLog(LogService.MQTT_CONNECTION_FAILURE, e.Exception.Message);
        }

        /// <summary>
        /// Disconnects from the platform.
        /// </summary>
        /// <param name="e"></param>
        private void OnMqttClientDisconnected(MqttClientDisconnectedEventArgs e)
        {
            Log.Error("Connect lost, deviceid is " + clientConf.DeviceId);
            if (connectListener != null)
            {
                connectListener.ConnectionLost();
            }
            
            SaveLog(LogService.MQTT_CONNECTION_LOST, e.Exception?.Message);
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
                    Log.Error($"rawMessageListener is null");

                    return;
                }

                RawMessage rawMessage = new RawMessage(e.ApplicationMessage.Id.ToString(), e.ApplicationMessage.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.ApplicationMessage.Payload));

                if (e.HasFailed)
                {
                    Log.Info($"messageId " + e.ApplicationMessage.Id + ", topic: " + e.ApplicationMessage.ApplicationMessage.Topic + ", payload: " + Encoding.UTF8.GetString(e.ApplicationMessage.ApplicationMessage.Payload) + " is published fail");

                    rawMessageListener.OnMessageUnPublished(rawMessage);
                }
                else if (e.HasSucceeded)
                {
                    Log.Info($"messageId " + e.ApplicationMessage.Id + ", topic: " + e.ApplicationMessage.ApplicationMessage.Topic + ", payload: " + Encoding.UTF8.GetString(e.ApplicationMessage.ApplicationMessage.Payload) + " is published success");
                    
                    rawMessageListener.OnMessagePublished(rawMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error("SDK.Error: the message is " + Encoding.UTF8.GetString(e.ApplicationMessage.ApplicationMessage.Payload));
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
                Dictionary<string, LogInfo> logDic = JsonUtil.ConvertJsonStringToDic<string, LogInfo>(IotUtil.ReadJsonFile(CommonFilePath.LOG_PATH));

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
                Log.Error("SDK.Error: SaveLog error");
            }
        }

        /// <summary>
        /// Get mqtt client options
        /// </summary>
        /// <returns></returns>
        private IManagedMqttClientOptions GetOptions()
        {
            IManagedMqttClientOptions options = null;

            string caCertPath = @"\certificate\DigiCertGlobalRootCA.crt.pem";

            string timestamp = DateTime.Now.ToString("yyyyMMddHH");
            string clientId = string.Empty;

            if (clientConf.ScopeId == null)
            {
                clientId = clientConf.DeviceId + "_" + clientConf.Mode + "_" + CheckTimestamp + "_" + timestamp;
            }
            else
            {
                clientId = clientConf.DeviceId + "_" + ConnectType + "_" + clientConf.ScopeId;
            }

            // Encrypts the secret using HMAC-SHA256.
            string secret = string.Empty;
            if (!string.IsNullOrEmpty(clientConf.Secret))
            {
                secret = EncryptUtil.HmacSHA256(clientConf.Secret, timestamp);
            }

            // Checks whether the connection is secure.
            if (clientConf.Port == MQTT_PORT)
            {
                options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromDays(RECONNECT_TIME))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithTcpServer(clientConf.ServerUri, clientConf.Port)
                    .WithCommunicationTimeout(TimeSpan.FromSeconds(DEFAULT_CONNECT_TIMEOUT))
                    .WithCredentials(clientConf.DeviceId, secret)
                    .WithClientId(clientId)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(DEFAULT_KEEPLIVE))
                    .WithCleanSession(false)
                    .WithProtocolVersion(MqttProtocolVersion.V311)
                    .Build())
                .Build();
            }
            else if ((clientConf.Port == MQTTS_PORT) && clientConf.DeviceCert == null)
            {
                options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromDays(RECONNECT_TIME))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithTcpServer(clientConf.ServerUri, clientConf.Port)
                    .WithCommunicationTimeout(TimeSpan.FromSeconds(DEFAULT_CONNECT_TIMEOUT))
                    .WithCredentials(clientConf.DeviceId, secret)
                    .WithClientId(clientId)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(DEFAULT_KEEPLIVE))
                    .WithCleanSession(false)
                    .WithTls(new MqttClientOptionsBuilderTlsParameters()
                    {
                        AllowUntrustedCertificates = true,
                        UseTls = true,
                        Certificates = new List<X509Certificate> { IotUtil.GetCert(caCertPath) },
                        CertificateValidationHandler = delegate { return true; },
                        IgnoreCertificateChainErrors = false,
                        IgnoreCertificateRevocationErrors = false
                    })
                    .WithProtocolVersion(MqttProtocolVersion.V311)
                    .Build())
                .Build();
            }
            else
            {
                // Uses a certificate to connect to the platform.
                options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromDays(RECONNECT_TIME))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithTcpServer(clientConf.ServerUri, clientConf.Port)
                    .WithCommunicationTimeout(TimeSpan.FromSeconds(DEFAULT_CONNECT_TIMEOUT))
                    .WithCredentials(clientConf.DeviceId, string.Empty)
                    .WithClientId(clientId)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(DEFAULT_KEEPLIVE))
                    .WithCleanSession(false)
                    .WithTls(new MqttClientOptionsBuilderTlsParameters()
                    {
                        AllowUntrustedCertificates = true,
                        UseTls = true,
                        Certificates = new List<X509Certificate> { IotUtil.GetCert(caCertPath), clientConf.DeviceCert },
                        CertificateValidationHandler = delegate { return true; },
                        IgnoreCertificateChainErrors = false,
                        IgnoreCertificateRevocationErrors = false
                    })
                    .WithProtocolVersion(MqttProtocolVersion.V311)
                    .Build())
                .Build();
            }

            return options;
        }
    }
}
