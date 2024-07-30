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
using IoT.Device.Demo.Core;
using IoT.SDK.Device;
using IoT.SDK.Device.Bootstrap;
using IoT.SDK.Device.Client.Listener;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace IoT.Device.Demo
{
    public class BootstrapSample : AbstractFeatureTestSample, BootstrapMessageListener
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        private DemoAuthConfig authConfig;
        private BootstrapConfig bootstrapConfig;
        private BootstrapClient bootstrapClient;
        private IoTDevice device;

        public IoTDevice GetDevice()
        {
            return device;
        }

        private void TryBootstrap()
        {
            var bootstrapUri = authConfig.ServerAddress;
            var port = authConfig.ServerPort;
            var deviceId = authConfig.DeviceId;

            // Creates a device with the X509Certificate2.
            bootstrapClient = authConfig.AuthUseCert
                ? new BootstrapClient(bootstrapUri, port, deviceId, DemoDeviceHelper.ReadDeviceCert(authConfig),
                    bootstrapConfig?.ScopeId)
                : new BootstrapClient(bootstrapUri, port, deviceId, authConfig.DeviceSecret, bootstrapConfig?.ScopeId);

            bootstrapClient.bootstrapMessageListener = this;
            bootstrapClient.Bootstrap(bootstrapConfig?.ReportedData);
        }


        public void Start(DemoConfig deviceConfig)
        {
            authConfig = deviceConfig.AuthConfig;
            bootstrapConfig = deviceConfig.BootstrapConfig;
            TryBootstrap();
        }

        protected virtual void DeviceInitialized()
        {
        }

        public virtual void OnBootstrapMessage(string payload)
        {
            var obj = JObject.Parse(payload);
            var address = obj["address"]?.ToString();
            if (address == null) return;

            // 引导成功后关闭客户端
            bootstrapClient.Close();
            LOG.Info("bootstrap success, address:{}", address);

            var serverInfos = address.Split(':');
            var provisionAuthConfig =
                JsonConvert.DeserializeObject<DemoAuthConfig>(JsonConvert.SerializeObject(authConfig));
            provisionAuthConfig.DeviceId = authConfig.DeviceId;
            provisionAuthConfig.ServerAddress = serverInfos[0];
            provisionAuthConfig.ServerPort = int.Parse(serverInfos[1]);

            if (bootstrapConfig?.ScopeId == null)
            {
                //registered device provision, keep original credential info
            }
            else
            {
                // registered group provision
                var deviceSecret = (string)obj["deviceSecret"];
                if (deviceSecret != null)
                {
                    provisionAuthConfig.AuthUseCert = false;
                    provisionAuthConfig.DeviceSecret = deviceSecret;
                }
                else
                {
                    provisionAuthConfig.AuthUseCert = true;
                    provisionAuthConfig.DeviceCert = authConfig.DeviceCert;
                    provisionAuthConfig.DeviceCertPassword = authConfig.DeviceCertPassword;
                }
            }

            device = DemoDeviceHelper.CreateDevice(provisionAuthConfig);
            if (device.Init() != 0)
            {
                throw new ArgumentException($"device init failed.");
            }

            device.GetClient().bootstrapMessageListener = this;
            DeviceInitialized();
        }

        public void OnRetryBootstrapMessage()
        {
            //断开已有设备并重引导
            device?.GetClient().Close();
            TryBootstrap();
        }
    }
}