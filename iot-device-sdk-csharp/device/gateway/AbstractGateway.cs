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
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Gateway.Requests;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.SDK.Device.Gateway
{
    /// <summary>
    /// 抽象网关，实现了子设备管理，子设备消息转发功能
    /// </summary>
    public abstract class AbstractGateway : IoTDevice, ConnectListener
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private SubDevDiscoveryListener subDevDiscoveryListener;

        private SubDevicesPersistence subDevicesPersistence;

        /// <summary>
        /// 构造函数，通过设备密码认证
        /// </summary>
        /// <param name="subDevicesPersistence">子设备持久化，提供子设备信息保存能力</param>
        /// <param name="serverUri">平台访问地址</param>
        /// <param name="port">端口</param>
        /// <param name="deviceId">设备id</param>
        /// <param name="deviceSecret">设备秘钥</param>
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
        /// 根据设备标识码查询子设备
        /// </summary>
        /// <param name="nodeId">设备标识码</param>
        /// <returns>子设备信息</returns>
        public DeviceInfo GetSubDeviceByNodeId(string nodeId)
        {
            return subDevicesPersistence.GetSubDevice(nodeId);
        }

        public void ConnectComplete()
        {
            // 建连或重连时，向平台同步子设备信息
            SyncSubDevices();
        }

        public void ConnectFail()
        {
        }

        public void ConnectionLost()
        {
        }
        
        /// <summary>
        /// 上报子设备消息
        /// </summary>
        /// <param name="deviceMessage">设备消息</param>
        public void ReportSubDeviceMessage(DeviceMessage deviceMessage)
        {
            GetClient().ReportDeviceMessage(deviceMessage);
        }

        /// <summary>
        /// 上报子设备属性
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="services">服务属性列表</param>
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
        /// 批量上报子设备属性
        /// </summary>
        /// <param name="deviceProperties">子设备属性列表</param>
        public void ReportSubDeviceProperties(List<DeviceProperty> deviceProperties)
        {
            string msg = "{\"devices\":" + JsonUtil.ConvertObjectToJsonString(deviceProperties) + "}";

            GetClient().Report(new PubMessage(CommonTopic.TOPIC_SYS_GATEWAY_SUB_DEVICES, msg));
        }

        /// <summary>
        /// 上报子设备状态
        /// </summary>
        /// <param name="deviceId">子设备ID</param>
        /// <param name="status">设备状态</param>
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
        /// 批量上报子设备状态
        /// </summary>
        /// <param name="statuses">子设备状态列表</param>
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
        /// 添加子设备处理回调，子类可以重写此接口进行扩展
        /// </summary>
        /// <param name="subDevicesInfo">子设备信息</param>
        /// <returns>处理结果，0表示成功</returns>
        public int OnAddSubDevices(SubDevicesInfo subDevicesInfo)
        {
            if (subDevicesPersistence != null)
            {
                return subDevicesPersistence.AddSubDevices(subDevicesInfo);
            }

            return -1;
        }

        /// <summary>
        /// 删除子设备处理回调，子类可以重写此接口进行扩展
        /// </summary>
        /// <param name="subDevicesInfo">子设备信息</param>
        /// <returns>处理结果，0表示成功</returns>
        public virtual int OnDeleteSubDevices(SubDevicesInfo subDevicesInfo)
        {
            if (subDevicesPersistence != null)
            {
                return subDevicesPersistence.DeleteSubDevices(subDevicesInfo);
            }

            return -1;
        }
        
        /// <summary>
        /// 子设备命令下发处理，网关需要转发给子设备，需要子类实现
        /// </summary>
        /// <param name="requestId">请求Id</param>
        /// <param name="command">命令</param>
        public abstract void OnSubdevCommand(string requestId, Command command);

        /// <summary>
        /// 子设备属性设置，网关需要转发给子设备，需要子类实现
        /// </summary>
        /// <param name="requestId">请求ID</param>
        /// <param name="propsSet">属性设置</param>
        public abstract void OnSubdevPropertiesSet(string requestId, PropsSet propsSet);

        /// <summary>
        /// 子设备读属性，网关需要转发给子设备，需要子类实现
        /// </summary>
        /// <param name="requestId">请求ID</param>
        /// <param name="propsGet">属性查询</param>
        public abstract void OnSubdevPropertiesGet(string requestId, PropsGet propsGet);

        /// <summary>
        /// 子设备消息下发，网关需要转发给子设备，需要子类实现
        /// </summary>
        /// <param name="message">设备消息</param>
        public abstract void OnSubdevMessage(DeviceMessage message);

        /// <summary>
        /// 向平台请求同步子设备信息
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
