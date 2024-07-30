/*
 * Copyright (c) 2023-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using IoT.Device.Demo.HubDevice;
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Gateway;
using IoT.SDK.Device.Gateway.Requests;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.Device.Demo.Gateway
{
    /// <summary>
    /// This example demonstrates how to use a CIG for TCP device access. The TCP device communicates with the platform through a gateway. An MQTT connection is set up between the gateway and platform.
    /// In this example, the TCP server (gateway) transmits simple character strings and sends a node ID for authentication in the first message. 
    /// You can extend the GatewayTcpServer class to implement more complex TCP servers.
    /// </summary>
    public class SimpleGateway : AbstractGateway
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        // Stores the mapping between the node ID and session.


        public ISessionManager GatewaySessionManager { get; }

        public SimpleGateway(SubDevicesPersistence subDevicesPersistence, string serverUri, int port, string deviceId,
            string deviceSecret) : base(subDevicesPersistence, serverUri, port, deviceId, deviceSecret)
        {
            GatewaySessionManager = new GatewaySessionManager(subDevicesPersistence);
        }

        public SimpleGateway(SubDevicesPersistence subDevicesPersistence, string serverUri, int port, string deviceId,
            X509Certificate deviceCert) : base(subDevicesPersistence, serverUri, port, deviceId, deviceCert)
        {
            GatewaySessionManager = new GatewaySessionManager(subDevicesPersistence);
        }


        public override void OnSubdevMessage(DeviceMessage message)
        {
            if (message.deviceId == null)
            {
                return;
            }

            string nodeId = IotUtil.GetNodeIdFromDeviceId(message.deviceId);
            if (nodeId == null)
            {
                return;
            }

            if (!GatewaySessionManager.GetSession(nodeId, out var session))
            {
                LOG.Error("session is null ,nodeId: {}", nodeId);
                return;
            }

            session.channel.WriteAndFlushAsync(message.content);
            LOG.Debug("flush device message {}", message.content);
        }

        public override void OnSubdevCommand(string requestId, Command command)
        {
            if (command.deviceId == null)
            {
                return;
            }

            string nodeId = IotUtil.GetNodeIdFromDeviceId(command.deviceId);
            if (nodeId == null)
            {
                return;
            }

            if (!GatewaySessionManager.GetSession(nodeId, out var session))
            {
                LOG.Error("session is null ,nodeId is {}", nodeId);
                return;
            }

            // In this example, the command object is directly converted to a string and sent to the child device. In actual scenarios, encoding and decoding may be required.
            session.channel.WriteAndFlushAsync(
                $"requestId:{requestId}, command:{JsonUtil.ConvertObjectToJsonString(command)}\n");
            LOG.Debug("flush command {}", command);
        }

        public override void OnSubdevPropertiesSet(string requestId, PropsSet propsSet)
        {
            if (propsSet.deviceId == null)
            {
                return;
            }

            string nodeId = IotUtil.GetNodeIdFromDeviceId(propsSet.deviceId);
            if (nodeId == null)
            {
                return;
            }

            if (!GatewaySessionManager.GetSession(nodeId, out var session))
            {
                LOG.Error("session is null, nodeId:{}", nodeId);
                return;
            }

            // In this example, the command object is directly converted to a string and sent to the child device. In actual scenarios, encoding and decoding may be required.
            session.channel.WriteAndFlushAsync(JsonUtil.ConvertObjectToJsonString(propsSet));
            LOG.Info("flush properties set {}", propsSet);

            // In this example, a command response is directly returned. It is more reasonable to send a respond after the gateway receives a response form the child device.
            GetClient().RespondPropsSet(requestId, IotResult.SUCCESS);
        }

        public override void OnSubdevPropertiesGet(string requestId, PropsGet propsGet)
        {
            // It is not recommended that the platform directly read child device properties. Therefore, a failure message is returned.
            LOG.Error("not supporte onSubdevPropertiesGet");
            GetClient().RespondPropsSet(requestId, IotResult.FAIL);
        }

        public override int OnDeleteSubDevices(SubDevicesInfo subDevicesInfo)
        {
            foreach (DeviceInfo subdevice in subDevicesInfo.devices)
            {
                if (GatewaySessionManager.RemoveSession(subdevice.nodeId, out Session session))
                {
                    if (session.channel != null)
                    {
                        session.channel.CloseAsync();
                    }
                }
            }

            return base.OnDeleteSubDevices(subDevicesInfo);
        }


        public override void OnSubdevEvent(string subDeviceId, DeviceEvent deviceEvent)
        {
            if (subDeviceId == null)
            {
                return;
            }

            string nodeId = IotUtil.GetNodeIdFromDeviceId(subDeviceId);
            if (nodeId == null)
            {
                LOG.Error("nodeId is null");
                return;
            }

            if (!GatewaySessionManager.GetSession(nodeId, out var session))
            {
                LOG.Error("session is null ,nodeId:{}", nodeId);
                return;
            }

            session.channel.WriteAndFlushAsync(JsonUtil.ConvertObjectToJsonString(deviceEvent));
            DeviceEvent deviceReportEvent = new DeviceEvent();
            if (deviceEvent.serviceId == "$ota")
            {
                deviceReportEvent.serviceId = "$ota";
                Dictionary<string, object> para = new Dictionary<string, object>();
                if (deviceEvent.eventType == "version_query")
                {
                    deviceReportEvent.eventType = "version_report";
                    para.Add("fw_version", "v1.0");
                }

                if (deviceEvent.eventType == "firmware_upgrade" || deviceEvent.eventType == "firmware_upgrade_v2")
                {
                    deviceReportEvent.eventType = "upgrade_progress_report";
                    para.Add("result_code", 0);
                    para.Add("version", deviceEvent.paras["version"]);
                }

                deviceReportEvent.paras = para;

                GetClient().ReportEvent(subDeviceId, deviceReportEvent);
            }
        }
    }
}