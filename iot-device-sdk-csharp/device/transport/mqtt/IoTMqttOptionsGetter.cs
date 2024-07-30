/*
 * Copyright (c) 2024-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Security.Cryptography.X509Certificates;
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Utils;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;

namespace IoT.SDK.Device.Transport.Mqtt
{
    public class IoTMqttOptionsGetter
    {
        private const string DEFAULT_IOT_CA_CERT = @"config\certificate\DigiCertGlobalRootCA.crt.pem";
        private const int MQTT_PORT = 1883;
        private const ushort DEFAULT_KEEPALIVE = 120;
        private const double DEFAULT_CONNECT_TIMEOUT = 30;

        private readonly ClientConf clientConf;

        public IoTMqttOptionsGetter(ClientConf clientConf)
        {
            this.clientConf = clientConf;
        }

        public IMqttClientOptions GetOptions()
        {
            var optionsBuilder = new MqttClientOptionsBuilder();

            // Checks whether using tls connection.
            var isUsingTls = clientConf.Port != MQTT_PORT;
            // using secret or cert
            var isUsingClientCert = isUsingTls && clientConf.DeviceCert != null;


            if (isUsingTls)
            {
                var clientCerts = new List<X509Certificate>
                {
                    clientConf.IotCaCert ?? IotUtil.GetCert(DEFAULT_IOT_CA_CERT)
                };

                if (isUsingClientCert)
                {
                    clientCerts.Add(clientConf.DeviceCert);
                }

                optionsBuilder.WithTls(new MqttClientOptionsBuilderTlsParameters()
                {
                    AllowUntrustedCertificates = true,
                    UseTls = true,
                    Certificates = clientCerts,
                    CertificateValidationHandler = delegate { return true; },
                    IgnoreCertificateChainErrors = false,
                    IgnoreCertificateRevocationErrors = false
                });
            }

            string timestamp = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now, TimeZoneInfo.Local)
                .ToString("yyyyMMddHH");
            // Encrypts the secret using HMAC-SHA256.
            string secret = String.Empty;
            string clientId;
            if (clientConf.ScopeId == null)
            {
                clientId = $"{clientConf.DeviceId}_{clientConf.Mode}_{clientConf.CheckTimestamp}_{timestamp}";
                if (!isUsingClientCert)
                {
                    secret = EncryptUtil.HmacSHA256(clientConf.Secret, timestamp);
                }
            }
            else
            {
                clientId = $"{clientConf.DeviceId}_{ClientConf.CONNECT_OF_NORMAL_DEVICE_MODE}_{clientConf.ScopeId}";
                if (!isUsingClientCert)
                {
                    clientId += $"_{clientConf.CheckTimestamp}_{timestamp}";
                    secret = EncryptUtil.HmacSHA256(
                        EncryptUtil.HmacSHA256(Convert.FromBase64String(clientConf.Secret), clientConf.DeviceId),
                        timestamp);
                }
            }

            optionsBuilder
                .WithCredentials(clientConf.DeviceId, secret)
                .WithTcpServer(clientConf.ServerUri, clientConf.Port)
                .WithCommunicationTimeout(TimeSpan.FromSeconds(DEFAULT_CONNECT_TIMEOUT))
                .WithClientId(clientId)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(DEFAULT_KEEPALIVE))
                .WithCleanSession()
                .WithProtocolVersion(clientConf.UseMqttV5 ? MqttProtocolVersion.V500 : MqttProtocolVersion.V311);
            return optionsBuilder.Build();
        }

        public IManagedMqttClientOptions GetManagedOptions()
        {
            IManagedMqttClientOptions options = new ManagedMqttClientOptionsBuilder()
                // we have custom reconnect hack in CustomReconnectDelayMqttClientProxy 
                // so here is set to small delay
                .WithClientOptions(GetOptions())
                .WithAutoReconnectDelay(clientConf.AutoReconnect ? TimeSpan.FromMilliseconds(0) : System.Threading.Timeout.InfiniteTimeSpan)
                .Build();
            return options;
        }
    }
}