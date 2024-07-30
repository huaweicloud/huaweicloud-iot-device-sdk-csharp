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

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Gateway.Requests;
using IoT.SDK.Device.Log;
using IoT.SDK.Device.OTA;
using IoT.SDK.Device.Service.Anomaly;
using IoT.SDK.Device.Service.DeviceConfig;
using IoT.SDK.Device.Service.DeviceRule;
using IoT.SDK.Device.Service.FileManager;
using IoT.SDK.Device.Service.TunnelManager;
using IoT.SDK.Device.Timesync;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace IoT.SDK.Device.Service
{
    /// <summary>
    /// An abstract device class.
    /// </summary>
    public abstract class AbstractDevice
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly DeviceClient client;

        private readonly Dictionary<string, AbstractService> services = new Dictionary<string, AbstractService>();

        public string deviceId { get; set; }

        public OTAService otaService { get; set; }

        public TimeSyncService timeSyncService { get; set; }

        public FileManagerService FileManagerService { get; set; }

        public LogService LogService { get; set; }

        public DeviceRuleService DeviceRuleService { get; set; }

        public DeviceConfigService DeviceConfigService { get; set; }

        public SecurityDetectionService SecurityDetectionService { get; set; }

        internal void CatchPropertiesForDeviceRuleProperties(PubMessage msg)
        {
            if (msg.Topic == CommonTopic.TOPIC_PROPERTIES_REPORT)
            {
                var obj = JsonConvert.DeserializeObject<JObject>(msg.Message);
                var o = obj["services"]?.ToObject<List<ServiceProperty>>();
                this.DeviceRuleService.CacheProperties(new List<DeviceProperty>
                {
                    new DeviceProperty
                    {
                        deviceId = deviceId,
                        services = o
                    }
                });
            }
            else if (msg.Topic == CommonTopic.TOPIC_SYS_GATEWAY_SUB_DEVICES)
            {
                var obj = JsonConvert.DeserializeObject<JObject>(msg.Message);
                var o = obj["devices"]?.ToObject<List<DeviceProperty>>();
                this.DeviceRuleService.CacheProperties(o);
            }
        }

        /// <summary>
        /// Constructor used to create an AbstractDevice object. In this method, secret authentication is used.
        /// </summary>
        /// <param name="serverUri">Indicates the device access address, for example, iot-mqtts.cn-north-4.myhuaweicloud.com.</param>
        /// <param name="port">Indicates the port for device access.</param>
        /// <param name="deviceId">Indicates a device ID.</param>
        /// <param name="deviceSecret">Indicates a secret.</param>
        public AbstractDevice(string serverUri, int port, string deviceId, string deviceSecret)
        {
            ClientConf clientConf = new ClientConf();
            clientConf.ServerUri = serverUri;
            clientConf.Port = port;
            clientConf.DeviceId = deviceId;
            clientConf.Secret = deviceSecret;
            this.deviceId = deviceId;
            this.client = new DeviceClient(clientConf, this);
            InitSysServices();
            LOG.Info("create device:{}", clientConf.DeviceId);
        }

        /// <summary>
        /// Constructor used to create an AbstractDevice object. In this method, certificate authentication is used.
        /// </summary>
        /// <param name="serverUri"></param>
        /// <param name="port"></param>
        /// <param name="deviceId"></param>
        /// <param name="deviceCert"></param>
        public AbstractDevice(string serverUri, int port, string deviceId, X509Certificate deviceCert)
        {
            ClientConf clientConf = new ClientConf();
            clientConf.ServerUri = serverUri;
            clientConf.Port = port;
            clientConf.DeviceId = deviceId;
            clientConf.DeviceCert = deviceCert;
            this.deviceId = deviceId;
            this.client = new DeviceClient(clientConf, this);
            this.InitSysServices();
            LOG.Info("create device:{}", clientConf.DeviceId);
        }


        /// <summary>
        /// Adds a service. You can use AbstractService to define your device service and add the service to the device.
        /// </summary>
        /// <param name="serviceId">Indicates a service ID, which must be defined in the device model.</param>
        /// <param name="deviceService">Indicates the service to add.</param>
        public void AddService(string serviceId, AbstractService deviceService)
        {
            deviceService.iotDevice = this;
            deviceService.InnerServiceId = serviceId;
            services.Add(serviceId, deviceService);
        }

        /// <summary>
        /// Adds a service. You can use AbstractService to define your device service and add the service to the device.
        /// the Service name is now provided by override the <c>GetServiceId()</c> of AbstractService  
        /// </summary>
        /// <param name="deviceService">Indicates the service to add.</param>
        public void AddService(AbstractService deviceService)
        {
            deviceService.iotDevice = this;
            services.Add(deviceService.GetServiceId(), deviceService);
        }

        /// <summary>
        /// Creates a connection to the platform.
        /// </summary>
        /// <returns>Returns 0 if the connection is successful; returns -1 otherwise.</returns>
        public int Init()
        {
            DeviceRuleService.Init();
            int ret = client.Connect();
            DeviceRuleService.AfterConnected();
            SecurityDetectionService.AfterConnected();
            return ret;
        }

        /// <summary>
        /// Obtains a device client. After a device client is obtained, you can call the message, property, and message APIs provided by the device client.
        /// </summary>
        /// <returns>Returns a DeviceClient instance.</returns>
        public virtual DeviceClient GetClient()
        {
            return client;
        }

        /// <summary>
        /// Called when a command is received. This method is automatically called by the SDK.
        /// </summary>
        /// <param name="requestId">Indicates a request ID.</param>
        /// <param name="command">Indicates a command.</param>
        public virtual void OnCommand(string requestId, Command command)
        {
            IService service = this.GetService(command.serviceId);

            if (service != null)
            {
                CommandRsp rsp = service.OnCommand(command);
                client.RespondCommand(requestId, rsp);
            }
        }


        /// <summary>
        /// Obtains a service.
        /// </summary>
        /// <param name="serviceId">Indicates the service ID.</param>
        /// <returns>Returns an AbstractService instance.</returns>
        public AbstractService GetService(string serviceId)
        {
            if (!services.ContainsKey(serviceId))
            {
                return null;
            }

            return services[serviceId];
        }

        /// <summary>
        /// Called when events are received. This method is automatically called by the SDK.
        /// </summary>
        /// <param name="deviceEvents">Indicates the events.</param>
        public virtual void OnEvent(DeviceEvents deviceEvents)
        {
            // For a sub device
            if (deviceEvents.deviceId != null && deviceEvents.deviceId != this.deviceId)
            {
                return;
            }

            foreach (DeviceEvent evnt in deviceEvents.services)
            {
                IService deviceService = this.GetService(evnt.serviceId);
                if (deviceService != null)
                {
                    deviceService.OnEvent(evnt);
                }
            }
        }

        /// <summary>
        /// Called when a property query request is received. This method is automatically called by the SDK.
        /// </summary>
        /// <param name="requestId">Indicates a request ID.</param>
        /// <param name="propsGet">Indicates the property query request.</param>
        public virtual void OnPropertiesGet(string requestId, PropsGet propsGet)
        {
            List<ServiceProperty> serviceProperties = new List<ServiceProperty>();

            // Queries all service IDs.
            if (propsGet.serviceId == null)
            {
                foreach (KeyValuePair<string, AbstractService> kv in services)
                {
                    IService deviceService = GetService(kv.Key);
                    if (deviceService != null)
                    {
                        Dictionary<string, object> properties = deviceService.OnRead();
                        ServiceProperty serviceProperty = new ServiceProperty();
                        serviceProperty.properties = properties;
                        serviceProperty.serviceId = kv.Key;
                        serviceProperties.Add(serviceProperty);
                    }
                }
            }
            else
            {
                IService deviceService = GetService(propsGet.serviceId);

                if (deviceService != null)
                {
                    Dictionary<string, object> properties = deviceService.OnRead();
                    ServiceProperty serviceProperty = new ServiceProperty();
                    serviceProperty.properties = properties;
                    serviceProperty.serviceId = propsGet.serviceId;
                    serviceProperties.Add(serviceProperty);
                }
            }

            client.RespondPropsGet(requestId, serviceProperties);
        }

        /// <summary>
        /// Called when a property setting request is received. This method is automatically called by the SDK.
        /// </summary>
        /// <param name="requestId">Indicates a request ID.</param>
        /// <param name="propsSet">Indicates the property setting request.</param>
        public virtual void OnPropertiesSet(string requestId, PropsSet propsSet)
        {
            foreach (ServiceProperty serviceProp in propsSet.services)
            {
                IService deviceService = GetService(serviceProp.serviceId);

                if (deviceService != null)
                {
                    // Returns the result for partial failure.
                    IotResult result = deviceService.OnWrite(serviceProp.properties);
                    if (result.resultCode != IotResult.SUCCESS.resultCode)
                    {
                        client.RespondPropsSet(requestId, result);

                        return;
                    }
                }
            }

            client.RespondPropsSet(requestId, IotResult.SUCCESS);
        }

        public virtual void OnDeviceMessage(DeviceMessage message)
        {
        }

        public virtual void OnDeviceShadow(string requestId, DeviceShadowResponse shadow)
        {
            foreach (var serviceProp in shadow.Shadow)
            {
                IService deviceService = GetService(serviceProp.ServiceId);
                if (deviceService == null) continue;
                // Returns the result for partial failure.
                IotResult result = deviceService.OnWrite(serviceProp.Desired.Properties);
                if (result.resultCode == IotResult.SUCCESS.resultCode) continue;
                client.RespondPropsSet(requestId, result);
                return;
            }
        }

        /// <summary>
        /// Reports a property change for a specific service. The SDK reports the changed properties.
        /// </summary>
        /// <param name="serviceId">Indicates the service ID.</param>
        /// <param name="properties">Indicates the properties.</param>
        internal void FirePropertiesChanged(string serviceId, string[] properties)
        {
            AbstractService deviceService = GetService(serviceId);
            if (deviceService == null)
            {
                return;
            }

            Dictionary<string, object> props = deviceService.OnRead(properties);

            ServiceProperty serviceProperty = new ServiceProperty();
            serviceProperty.serviceId = deviceService.GetServiceId();
            serviceProperty.properties = props;
            serviceProperty.eventTime = IotUtil.GetEventTime();

            List<ServiceProperty> listProperties = new List<ServiceProperty>();
            listProperties.Add(serviceProperty);

            client.ReportProperties(listProperties);
        }

        /// <summary>
        /// Initializes the default system service, which starts with a dollar sign ($).
        /// </summary>
        private void InitSysServices()
        {
            this.otaService = new OTAService();
            this.AddService(otaService);
            this.timeSyncService = new TimeSyncService();
            this.AddService(timeSyncService);
            this.FileManagerService = new FileManagerService();
            this.AddService(FileManagerService);
            this.AddService(new TunnelManagerService());
            this.LogService = new LogService();
            this.AddService(this.LogService);
            this.DeviceRuleService = new DeviceRuleService();
            this.AddService(DeviceRuleService);
            this.DeviceConfigService = new DeviceConfigService();
            this.AddService(DeviceConfigService);
            this.SecurityDetectionService = new SecurityDetectionService();
            this.AddService(SecurityDetectionService);
        }
    }
}