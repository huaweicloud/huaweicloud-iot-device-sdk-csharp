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

using System.Collections.Generic;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace IoT.SDK.Device.Transport.Mqtt
{
    internal class CustomReconnectDelayMqttFactoryProxy : IMqttFactory
    {
        private readonly IMqttFactory factory;
        private readonly Connection connection;
        private readonly IoTMqttOptionsGetter optionsGetter;

        public CustomReconnectDelayMqttFactoryProxy(IMqttFactory factory, Connection connection,
            IoTMqttOptionsGetter optionsGetter)
        {
            this.factory = factory;
            this.connection = connection;
            this.optionsGetter = optionsGetter;
        }

        public IMqttFactory UseClientAdapterFactory(IMqttClientAdapterFactory clientAdapterFactory)
        {
            return factory.UseClientAdapterFactory(clientAdapterFactory);
        }

        public IMqttClient CreateMqttClient()
        {
            var client = new CustomReconnectDelayMqttClientProxy(factory.CreateMqttClient(), optionsGetter);
            connection.AddConnectListener(client);
            return client;
        }

        public IMqttClient CreateMqttClient(IMqttNetLogger logger)
        {
            var client = new CustomReconnectDelayMqttClientProxy(factory.CreateMqttClient(logger), optionsGetter);
            connection.AddConnectListener(client);
            return client;
        }

        public IMqttClient CreateMqttClient(IMqttClientAdapterFactory adapterFactory)
        {
            var client =
                new CustomReconnectDelayMqttClientProxy(factory.CreateMqttClient(adapterFactory), optionsGetter);
            connection.AddConnectListener(client);
            return client;
        }

        public IMqttClient CreateMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory adapterFactory)
        {
            var client =
                new CustomReconnectDelayMqttClientProxy(factory.CreateMqttClient(logger, adapterFactory),
                    optionsGetter);
            connection.AddConnectListener(client);
            return client;
        }

        public IMqttServer CreateMqttServer()
        {
            return factory.CreateMqttServer();
        }

        public IMqttServer CreateMqttServer(IMqttNetLogger logger)
        {
            return factory.CreateMqttServer(logger);
        }

        public IMqttServer CreateMqttServer(IEnumerable<IMqttServerAdapter> adapters)
        {
            return factory.CreateMqttServer(adapters);
        }

        public IMqttServer CreateMqttServer(IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger)
        {
            return factory.CreateMqttServer(adapters, logger);
        }

        public IMqttNetLogger DefaultLogger => factory.DefaultLogger;
    }
}