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
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Client.Connecting;
using MQTTnet.Client;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using NLog;

namespace IoT.SDK.Device.Transport.Mqtt
{
    internal class CustomReconnectDelayMqttClientProxy : IMqttClient, ConnectListener
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly IMqttClient mqttClient;

        private readonly IoTMqttOptionsGetter optionsGetter;

        private readonly ExponentialBackoff exponentialBackoff = new ExponentialBackoff
        {
            MinBackoff = 1000,
            MaxBackoff = 5 * 60 * 1000, // 5 minutes
            DefaultBackoff = 1000
        };

        public CustomReconnectDelayMqttClientProxy(IMqttClient mqttClient, IoTMqttOptionsGetter optionsGetter)
        {
            this.mqttClient = mqttClient;
            this.optionsGetter = optionsGetter;
        }

        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler
        {
            get => mqttClient.ApplicationMessageReceivedHandler;
            set => mqttClient.ApplicationMessageReceivedHandler = value;
        }

        private async Task<MqttClientAuthenticateResult> ConnectWithBackoffDelayAsync(
            CancellationToken cancellationToken)
        {
            if (exponentialBackoff.IsCleared)
            {
                LOG.Info("connecting");
            }
            else
            {
                // Backoff reconnection
                var delayTime = exponentialBackoff.TimeDelay;
                LOG.Info("reconnect after {}ms", delayTime);
                await Task.Delay(TimeSpan.FromMilliseconds(delayTime), cancellationToken);
            }

            return await mqttClient.ConnectAsync(optionsGetter.GetOptions(), cancellationToken);
        }

        public Task<MqttClientAuthenticateResult> ConnectAsync(IMqttClientOptions options,
            CancellationToken cancellationToken)
        {
            return ConnectWithBackoffDelayAsync(cancellationToken);
        }

        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage,
            CancellationToken cancellationToken)
        {
            return mqttClient.PublishAsync(applicationMessage, cancellationToken);
        }


        public void Dispose()
        {
            mqttClient.Dispose();
        }


        public Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken)
        {
            return mqttClient.DisconnectAsync(options, cancellationToken);
        }

        public Task PingAsync(CancellationToken cancellationToken)
        {
            return mqttClient.PingAsync(cancellationToken);
        }

        public Task SendExtendedAuthenticationExchangeDataAsync(MqttExtendedAuthenticationExchangeData data,
            CancellationToken cancellationToken)
        {
            return mqttClient.SendExtendedAuthenticationExchangeDataAsync(data, cancellationToken);
        }

        public Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options,
            CancellationToken cancellationToken)
        {
            return mqttClient.SubscribeAsync(options, cancellationToken);
        }

        public Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options,
            CancellationToken cancellationToken)
        {
            return mqttClient.UnsubscribeAsync(options, cancellationToken);
        }

        public bool IsConnected => mqttClient.IsConnected;
        public IMqttClientOptions Options => mqttClient.Options;

        public IMqttClientConnectedHandler ConnectedHandler
        {
            get => mqttClient.ConnectedHandler;
            set => mqttClient.ConnectedHandler = value;
        }

        public IMqttClientDisconnectedHandler DisconnectedHandler
        {
            get => mqttClient.DisconnectedHandler;
            set => mqttClient.DisconnectedHandler = value;
        }


        public void ConnectionLost()
        {
        }

        public void ConnectComplete()
        {
            exponentialBackoff.Clear();
        }

        public void ConnectFail()
        {
            exponentialBackoff.IncAdverseEvent();
        }
    }
}