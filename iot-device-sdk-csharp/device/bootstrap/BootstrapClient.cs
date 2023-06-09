/*
 * Copyright (c) 2022-2022 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Security.Cryptography.X509Certificates;
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Transport.Mqtt;
using MQTTnet;
using NLog;

namespace IoT.SDK.Device.Bootstrap
{
    public class BootstrapClient : RawMessageListener, ConnectListener
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private string deviceId;

        private Connection connection;
        
        /// <summary>
        /// Constructor used to create an BootstrapClient object. In this method, secret authentication is used.
        /// </summary>
        /// <param name="bootstrapUri">Indicates the bootstrap server access address, for example, ssl://iot-bs.cn-north-4.myhuaweicloud.com.</param>
        /// <param name="port">Indicates the port for device access.</param>
        /// <param name="deviceId">Indicates a device ID.</param>
        /// <param name="deviceSecret">Indicates a secret.</param>
        public BootstrapClient(string bootstrapUri, int port, string deviceId, string deviceSecret)
        {
            ClientConf clientConf = new ClientConf();
            clientConf.ServerUri = bootstrapUri;
            clientConf.Port = port;
            clientConf.DeviceId = deviceId;
            clientConf.Secret = deviceSecret;
            this.deviceId = deviceId;
            this.connection = new MqttConnection(clientConf, this, this);
            Log.Info("create BootstrapClient: " + clientConf.DeviceId);
        }

        /// <summary>
        /// Constructor used to create an BootstrapClient object. In this method, certificate authentication is used.
        /// </summary>
        /// <param name="bootstrapUri">Indicates the bootstrap server access address, for example, ssl://iot-bs.cn-north-4.myhuaweicloud.com.</param>
        /// <param name="port">Indicates the port for device access.</param>
        /// <param name="deviceId">Indicates a device ID.</param>
        /// <param name="deviceCert">Indicates the device certificate</param>
        public BootstrapClient(string bootstrapUri, int port, string deviceId, X509Certificate2 deviceCert)
        {
            ClientConf clientConf = new ClientConf();
            clientConf.ServerUri = bootstrapUri;
            clientConf.Port = port;
            clientConf.DeviceId = deviceId;
            clientConf.DeviceCert = deviceCert;
            this.deviceId = deviceId;
            this.connection = new MqttConnection(clientConf, this, this);
            Log.Info("create BootstrapClient: " + clientConf.DeviceId);
        }

        /// <summary>
        /// 构造函数，自注册场景下证书创建
        /// </summary>
        /// <param name="bootstrapUri">bootstrap server地址，比如ssl://iot-bs.cn-north-4.myhuaweicloud.com:8883</param>
        /// <param name="port">Indicates the port for device access.</param>
        /// <param name="deviceId">Indicates a device ID.</param>
        /// <param name="deviceCert">Indicates the device certificate</param>
        /// <param name="scopeId"></param>
        public BootstrapClient(string bootstrapUri, int port, string deviceId, X509Certificate2 deviceCert, string scopeId)
        {
            ClientConf clientConf = new ClientConf();
            clientConf.ServerUri = bootstrapUri;
            clientConf.Port = port;
            clientConf.DeviceId = deviceId;
            clientConf.ScopeId = scopeId;
            clientConf.DeviceCert = deviceCert;
            this.deviceId = deviceId;
            this.connection = new MqttConnection(clientConf, this, this);
            Log.Info("create BootstrapClient: " + clientConf.DeviceId);
        }

        public BootstrapMessageListener bootstrapMessageListener { get; set; }

        public void OnMessageReceived(RawMessage message)
        {
            if (message.Topic.Contains("/sys/bootstrap/down") && bootstrapMessageListener != null)
            {
                bootstrapMessageListener.OnBootstrapMessage(message.ToString());
            }
        }

        public void OnMessagePublished(RawMessage message)
        {
        }

        public void OnMessageUnPublished(RawMessage message)
        {
        }

        public void ConnectComplete()
        {
        }

        public void ConnectionLost()
        {
        }

        public void ConnectFail()
        {
        }

        public void Bootstrap()
        {
            if (connection.Connect() != 0)
            {
                Log.Error("connect failed.");

                return;
            }
            
            List<MqttTopicFilter> listTopic = new List<MqttTopicFilter>();
            var topicFilterBulderPreTopic = new MqttTopicFilterBuilder().WithTopic(string.Format(CommonTopic.TOPIC_SYS_BOOTSTRAP_DOWN, this.deviceId)).Build();
            listTopic.Add(topicFilterBulderPreTopic);
            connection.SubscribeTopic(listTopic);
            
            connection.PublishMessage(new RawMessage(string.Format(CommonTopic.TOPIC_SYS_BOOTSTRAP_UP, this.deviceId), string.Empty));
        }

        /// <summary>
        /// Close connection
        /// </summary>
        public void Close()
        {
            connection.Close();
        }
    }
}
