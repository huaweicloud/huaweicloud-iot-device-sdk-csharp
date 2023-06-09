/*
 * Copyright (c) 2020-2020 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Gateway.Requests;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.SDK.Device.Gateway
{
    /// <summary>
    /// An abstract gateway that implements device management and message forwarding of child devices.
    /// </summary>
    public abstract class AbstractGateway : IoTDevice, ConnectListener
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private SubDevDiscoveryListener subDevDiscoveryListener;

        private SubDevicesPersistence subDevicesPersistence;

        /// <summary>
        /// Constructor used to create an AbstractGateway object. In this method, secret authentication is used.
        /// </summary>
        /// <param name="subDevicesPersistence">Indicates the persistence of child device details.</param>
        /// <param name="serverUri">Indicates the device access address.</param>
        /// <param name="port">Indicates the port for device access.</param>
        /// <param name="deviceId">Indicates the device ID.</param>
        /// <param name="deviceSecret">Indicates the secret.</param>
        public AbstractGateway(SubDevicesPersistence subDevicesPersistence, string serverUri, int port, string deviceId, string deviceSecret) : base(serverUri, port, deviceId, deviceSecret)
        {
            this.subDevicesPersistence = subDevicesPersistence;

            GetClient().connectListener = this;
        }

        public AbstractGateway(SubDevicesPersistence subDevicesPersistence, string serverUri, int port, string deviceId, X509Certificate deviceCert) : base(serverUri, port, deviceId, deviceCert)
        {
            this.subDevicesPersistence = subDevicesPersistence;

            GetClient().connectListener = this;
        }

        /// <summary>
        /// Obtains a child device by node ID.
        /// </summary>
        /// <param name="nodeId">Indicates the node ID.</param>
        /// <returns>Returns the child device details.</returns>
        public DeviceInfo GetSubDeviceByNodeId(string nodeId)
        {
            return subDevicesPersistence.GetSubDevice(nodeId);
        }

        public void ConnectComplete()
        {
            // Synchronizes child device details to the platform during connection or reconnection.
            SyncSubDevices();
        }

        public void ConnectFail()
        {
        }

        public void ConnectionLost()
        {
        }

        /// <summary>
        /// Reports a child device message.
        /// </summary>
        /// <param name="deviceMessage">Indicates the message to report.</param>
        public void ReportSubDeviceMessage(DeviceMessage deviceMessage)
        {
            GetClient().ReportDeviceMessage(deviceMessage);
        }

        /// <summary>
        /// Reports properties for a child device.
        /// </summary>
        /// <param name="deviceId">Indicates the ID of the child device.</param>
        /// <param name="services">Indicates the properties to report.</param>
        public void ReportSubDeviceProperties(string deviceId, List<ServiceProperty> services)
        {
            DeviceProperty deviceProperty = new DeviceProperty();
            deviceProperty.deviceId = deviceId;
            deviceProperty.services = services;

            List<DeviceProperty> deviceProperties = new List<DeviceProperty>();
            deviceProperties.Add(deviceProperty);
            ReportSubDeviceProperties(deviceProperties);
        }

        /// <summary>
        /// Reports properties for a batches of child devices.
        /// </summary>
        /// <param name="deviceProperties">Indicates the properties to report.</param>
        public void ReportSubDeviceProperties(List<DeviceProperty> deviceProperties)
        {
            string msg = "{\"devices\":" + JsonUtil.ConvertObjectToJsonString(deviceProperties) + "}";

            GetClient().Report(new PubMessage(CommonTopic.TOPIC_SYS_GATEWAY_SUB_DEVICES, msg));
        }

        /// <summary>
        /// Reports the status for a child device.
        /// </summary>
        /// <param name="deviceId">Indicates the ID of the child device.</param>
        /// <param name="status">Indicates the status to report.</param>
        public void ReportSubDeviceStatus(string deviceId, string status)
        {
            DeviceStatus deviceStatus = new DeviceStatus();
            deviceStatus.deviceId = deviceId;
            deviceStatus.status = status;

            List<DeviceStatus> statuses = new List<DeviceStatus>();
            statuses.Add(deviceStatus);

            ReportSubDeviceStatus(statuses);
        }

        /// <summary>
        /// Reports the statuses for a batch of child devices.
        /// </summary>
        /// <param name="statuses">Indicates the statuses to report.</param>
        public void ReportSubDeviceStatus(List<DeviceStatus> statuses)
        {
            DeviceEvent deviceEvent = new DeviceEvent();
            deviceEvent.serviceId = "$sub_device_manager";
            deviceEvent.eventTime = IotUtil.GetTimeStamp();
            deviceEvent.eventType = "sub_device_update_status";

            Dictionary<string, object> para = new Dictionary<string, object>();
            para.Add("device_statuses", statuses);
            deviceEvent.paras = para;
            GetClient().ReportEvent(deviceEvent);
        }

        /// <summary>
        /// Add sub device on device side.
        /// </summary>
        /// <param name="subDeviceInfo">Indicates the list of sub device information to be added, with a maximum of 50 devices added at a time.</param>
        public void ReportAddSubDevice(List<DeviceInfo> subDeviceInfo)
        {
            DeviceEvent deviceEvent = new DeviceEvent();
            deviceEvent.serviceId = "$sub_device_manager";
            deviceEvent.eventTime = IotUtil.GetTimeStamp();
            deviceEvent.eventType = "add_sub_device_request";
            
            Dictionary<string, object> para = new Dictionary<string, object>();
            para.Add("devices", subDeviceInfo);
            deviceEvent.paras = para;
            GetClient().ReportEvent(deviceEvent);
        }

        /// <summary>
        /// Delete sub device on device side.
        /// </summary>
        /// <param name="devicesId">Indicates the list of sub devices (device ID) to be deleted, with a maximum of 50 devices to be deleted.</param>
        public void ReportDeleteSubDevice(List<string> devicesId)
        {
            DeviceEvent deviceEvent = new DeviceEvent();
            deviceEvent.serviceId = "$sub_device_manager";
            deviceEvent.eventTime = IotUtil.GetTimeStamp();
            deviceEvent.eventType = "delete_sub_device_request";

            Dictionary<string, object> para = new Dictionary<string, object>();
            para.Add("devices", devicesId);
            deviceEvent.paras = para;
            GetClient().ReportEvent(deviceEvent);
        }

        public override void OnEvent(DeviceEvents deviceEvents)
        {
            base.OnEvent(deviceEvents);

            foreach (DeviceEvent deviceEvent in deviceEvents.services)
            {
                if (deviceEvent.eventType == "start_scan")
                {
                    ScanSubdeviceNotify scanSubdeviceNotify = JsonUtil.ConvertDicToObject<ScanSubdeviceNotify>(
                        deviceEvent.paras);

                    if (subDevDiscoveryListener != null)
                    {
                        subDevDiscoveryListener.OnScan(scanSubdeviceNotify);
                    }
                }
                else if (deviceEvent.eventType == "add_sub_device_notify")
                {
                    SubDevicesInfo subDevicesInfo = JsonUtil.ConvertDicToObject<SubDevicesInfo>(
                        deviceEvent.paras);

                    OnAddSubDevices(subDevicesInfo);
                }
                else if (deviceEvent.eventType == "delete_sub_device_notify")
                {
                    SubDevicesInfo subDevicesInfo = JsonUtil.ConvertDicToObject<SubDevicesInfo>(
                        deviceEvent.paras);

                    OnDeleteSubDevices(subDevicesInfo);
                }
            }
        }

        /// <summary>
        /// Called when command processing.
        /// </summary>
        /// <param name="requestId">Indicates a request ID.</param>
        /// <param name="command">Indicates the command.</param>
        public override void OnCommand(string requestId, Command command)
        {
            // Sub device.
            if (command.deviceId != null && command.deviceId != this.deviceId)
            {
                this.OnSubdevCommand(requestId, command);

                return;
            }

            // Gateway
            base.OnCommand(requestId, command);
        }

        /// <summary>
        /// Called when a child device addition request is processed. Child classes can override this method.
        /// </summary>
        /// <param name="subDevicesInfo">Indicates the child device details.</param>
        /// <returns>Returns 0 if the processing is successful; returns other values if the processing fails.</returns>
        public int OnAddSubDevices(SubDevicesInfo subDevicesInfo)
        {
            if (subDevicesPersistence != null)
            {
                return subDevicesPersistence.AddSubDevices(subDevicesInfo);
            }

            return -1;
        }

        /// <summary>
        /// Called when a child device deletion request is processed. Child classes can override this method.
        /// </summary>
        /// <param name="subDevicesInfo">Indicates the child device details.</param>
        /// <returns>Returns 0 if the processing is successful; returns other values if the processing fails.</returns>
        public virtual int OnDeleteSubDevices(SubDevicesInfo subDevicesInfo)
        {
            if (subDevicesPersistence != null)
            {
                return subDevicesPersistence.DeleteSubDevices(subDevicesInfo);
            }

            return -1;
        }

        /// <summary>
        /// Called when a command delivered to a child device is processed. The gateway must forward such a command to the child device. This method must be implemented by the child class.
        /// </summary>
        /// <param name="requestId">Indicates a request ID.</param>
        /// <param name="command">Indicates the command.</param>
        public abstract void OnSubdevCommand(string requestId, Command command);

        /// <summary>
        /// Called when a property setting request delivered to a child device is processed. The gateway must forward such a request to the child device. This method must be implemented by the child class.
        /// </summary>
        /// <param name="requestId">Indicates a request ID.</param>
        /// <param name="propsSet">Indicates the properties to set.</param>
        public abstract void OnSubdevPropertiesSet(string requestId, PropsSet propsSet);

        /// <summary>
        /// Called when a property query request delivered to a child device is processed. The gateway must forward such a request to the child device. This method must be implemented by the child class.
        /// </summary>
        /// <param name="requestId">Indicates a request ID.</param>
        /// <param name="propsGet">Indicates the properties to query.</param>
        public abstract void OnSubdevPropertiesGet(string requestId, PropsGet propsGet);

        /// <summary>
        /// Called when a message delivered to a child device is processed. The gateway must forward such a message to the child device. This method must be implemented by the child class.
        /// </summary>
        /// <param name="message">Indicates the message.</param>
        public abstract void OnSubdevMessage(DeviceMessage message);

        /// <summary>
        /// Synchronizes child device details to the platform.
        /// </summary>
        protected void SyncSubDevices()
        {
            Log.Info("start to syncSubDevices, local version is " + subDevicesPersistence.GetVersion());

            DeviceEvent deviceEvent = new DeviceEvent();
            deviceEvent.eventType = "sub_device_sync_request";
            deviceEvent.serviceId = "sub_device_manager";
            deviceEvent.eventTime = IotUtil.GetTimeStamp();

            Dictionary<string, object> para = new Dictionary<string, object>();
            para.Add("version", subDevicesPersistence.GetVersion());
            deviceEvent.paras = para;
            GetClient().ReportEvent(deviceEvent);
        }
    }
}
