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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using IoT.SDK.Device.Client;
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

        private static readonly ushort RECONNECT_TIME = 5;

        private static readonly ushort DEFAULT_CONNECT_TIMEOUT = 60;

        private static ManualResetEvent mre = new ManualResetEvent(true);

        private static IManagedMqttClient client = null;

        private static IManagedMqttClientOptions options = null;

        private ClientConf clientConf;

        private ConnectListener connectListener;

        private RawMessageListener rawMessageListener;

        public MqttConnection(ClientConf clientConf, RawMessageListener rawMessageListener)
        {
            this.clientConf = clientConf;
            this.rawMessageListener = rawMessageListener;
        }
        
        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <returns></returns>
        public int Connect()
        {
            string caCertPath = @"\certificate\DigiCertGlobalRootCA.crt.pem";
            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMddHH");
                string clientID = clientConf.DeviceId + "_0_0_" + timestamp;

                // 对密码进行HmacSHA256加密
                string secret = string.Empty;
                if (!string.IsNullOrEmpty(clientConf.Secret))
                {
                    secret = EncryptUtil.HmacSHA256(clientConf.Secret, timestamp);
                }

                client = new MqttFactory().CreateManagedMqttClient();

                // 判断是否为安全连接
                if (clientConf.Port == 1883)
                {
                    options = new ManagedMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(RECONNECT_TIME))
                    .WithClientOptions(new MqttClientOptionsBuilder()
                        .WithTcpServer(clientConf.ServerUri, clientConf.Port)
                        .WithCommunicationTimeout(TimeSpan.FromSeconds(DEFAULT_CONNECT_TIMEOUT))
                        .WithCredentials(clientConf.DeviceId, secret)
                        .WithClientId(clientID)
                        .WithKeepAlivePeriod(TimeSpan.FromSeconds(DEFAULT_KEEPLIVE))
                        .WithCleanSession(false)
                        .WithProtocolVersion(MqttProtocolVersion.V311)
                        .Build())
                    .Build();
                }
                else if (clientConf.Port == 8883 && clientConf.DeviceCert == null)
                {
                    options = new ManagedMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(RECONNECT_TIME))
                    .WithClientOptions(new MqttClientOptionsBuilder()
                        .WithTcpServer(clientConf.ServerUri, clientConf.Port)
                        .WithCommunicationTimeout(TimeSpan.FromSeconds(DEFAULT_CONNECT_TIMEOUT))
                        .WithCredentials(clientConf.DeviceId, secret)
                        .WithClientId(clientID)
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
                    // 证书接入平台
                    options = new ManagedMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(RECONNECT_TIME))
                    .WithClientOptions(new MqttClientOptionsBuilder()
                        .WithTcpServer(clientConf.ServerUri, clientConf.Port)
                        .WithCommunicationTimeout(TimeSpan.FromSeconds(DEFAULT_CONNECT_TIMEOUT))
                        .WithCredentials(clientConf.DeviceId, string.Empty)
                        .WithClientId(clientID)
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

                // 注册事件
                client.ApplicationMessageProcessedHandler = new ApplicationMessageProcessedHandlerDelegate(new Action<ApplicationMessageProcessedEventArgs>(ApplicationMessageProcessedHandlerMethod)); // 消息发布回调

                client.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(new Action<MqttApplicationMessageReceivedEventArgs>(MqttApplicationMessageReceived)); // 命令下发回调

                client.ConnectedHandler = new MqttClientConnectedHandlerDelegate(new Action<MqttClientConnectedEventArgs>(OnMqttClientConnected));

                client.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(new Action<MqttClientDisconnectedEventArgs>(OnMqttClientDisconnected)); // 连接断开回调

                client.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(new Action<ManagedProcessFailedEventArgs>(OnMqttClientConnectingFailed));

                Log.Info("try to connect to server " + clientConf.ServerUri);

                // 连接平台设备
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

            // 上行响应
            client.PublishAsync(appMsg).Wait();

            Log.Debug($"publish msg : publishing message is " + message.Payload);
        }

        public void Close()
        {
            if (client.IsConnected)
            {
                try
                {
                    client.StopAsync().Wait();
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
                    Log.Debug("MQTT客户端尚未连接!");
                    return;
                }
                
                if (listTopic.Count <= 0)
                {
                    Log.Debug("订阅主题不能为空！");
                    return;
                }

                // 订阅Topic
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
        /// 接收到消息
        /// </summary>
        /// <param name="e"></param>
        private void MqttApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            Log.Info("messageArrived topic =  " + e.ApplicationMessage.Topic + ", msg = " + Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
            RawMessage rawMessage = new RawMessage(e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
            try
            {
                if (rawMessageListener != null)
                {
                    rawMessageListener.OnMessageReceived(rawMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error("SDK.Error: Message received error, the message is " + e.ApplicationMessage.Payload);
            }
        }

        private void OnMqttClientConnected(MqttClientConnectedEventArgs e)
        {
            Log.Debug("connect success, deviceid is " + clientConf.DeviceId);
            if (connectListener != null)
            {
                connectListener.ConnectComplete();
            }
            mre.Set();
        }

        private void OnMqttClientConnectingFailed(ManagedProcessFailedEventArgs e)
        {
            try
            {
                Log.Error("SDK.Error: Connect fail, deviceid is " + clientConf.DeviceId);
                if (connectListener != null)
                {
                    connectListener.ConnectFail();
                }
            }
            catch (Exception ex)
            {
                Log.Error("SDK.Error: Connect fail, deviceid is " + clientConf.DeviceId);
            }
            mre.Set();
        }

        /// <summary>
        /// 断开服务器连接
        /// </summary>
        /// <param name="e"></param>
        private void OnMqttClientDisconnected(MqttClientDisconnectedEventArgs e)
        {
            Log.Error("Connect lost, deviceid is " + clientConf.DeviceId);
            if (connectListener != null)
            {
                connectListener.ConnectionLost();
            }
        }

        /// <summary>
        /// 消息发布回调
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
                    Log.Debug($"messageId " + e.ApplicationMessage.Id + ", Topic: " + e.ApplicationMessage.ApplicationMessage.Topic + ", Payload: " + Encoding.UTF8.GetString(e.ApplicationMessage.ApplicationMessage.Payload) + " is published fail");

                    rawMessageListener.OnMessageUnPublished(rawMessage);
                }
                else if (e.HasSucceeded)
                {
                    Log.Debug($"messageId " + e.ApplicationMessage.Id + ", Topic: " + e.ApplicationMessage.ApplicationMessage.Topic + ", Payload: " + Encoding.UTF8.GetString(e.ApplicationMessage.ApplicationMessage.Payload) + " is published success");
                    
                    rawMessageListener.OnMessagePublished(rawMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error("SDK.Error: the message is " + Encoding.UTF8.GetString(e.ApplicationMessage.ApplicationMessage.Payload));
            }
        }
    }
}
