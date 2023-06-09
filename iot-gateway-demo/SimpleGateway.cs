using System;
using System.Collections.Generic;
using DotNetty.Transport.Channels;
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Gateway;
using IoT.SDK.Device.Gateway.Requests;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.Gateway.Demo
{
    /// <summary>
    /// This example demonstrates how to use a CIG for TCP device access. The TCP device communicates with the platform through a gateway. An MQTT connection is set up between the gateway and platform.
    /// In this example, the TCP server (gateway) transmits simple character strings and sends a node ID for authentication in the first message. 
    /// You can extend the StringTcpServer class to implement more complex TCP servers.
    /// </summary>
    public class SimpleGateway : AbstractGateway
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private Dictionary<string, Session> nodeIdToSesseionDic; // Stores the mapping between the node ID and session.

        private Dictionary<string, Session> channelIdToSessionDic; // Stores the mapping between the channel ID and session.

        public SimpleGateway(SubDevicesPersistence subDevicesPersistence, string serverUri, int port, string deviceId, string deviceSecret) : base(subDevicesPersistence, serverUri, port, deviceId, deviceSecret)
        {
            this.nodeIdToSesseionDic = new Dictionary<string, Session>();
            this.channelIdToSessionDic = new Dictionary<string, Session>();
        }

        public Session GetSessionByChannel(string channelId)
        {
            if (!channelIdToSessionDic.ContainsKey(channelId))
            {
                return null;
            }

            return channelIdToSessionDic[channelId];
        }

        public void RemoveSession(string channelId)
        {
            Session session = channelIdToSessionDic[channelId];
            if (session == null)
            {
                return;
            }

            channelIdToSessionDic.Remove(channelId);

            nodeIdToSesseionDic.Remove(session.nodeId);

            Log.Info("session removed " + session.ToString());
        }

        public Session CreateSession(string nodeId, IChannel channel)
        {
            // The child device has been added by calling a northbound API.
            DeviceInfo subdev = GetSubDeviceByNodeId(nodeId);
            if (subdev != null)
            {
                Session session = new Session();
                session.channel = channel;
                session.nodeId = nodeId;
                session.deviceId = subdev.deviceId;

                if (!nodeIdToSesseionDic.ContainsKey(nodeId))
                {
                    nodeIdToSesseionDic.Add(nodeId, session);
                }

                if (!channelIdToSessionDic.ContainsKey(channel.Id.AsLongText()))
                {
                    channelIdToSessionDic.Add(channel.Id.AsLongText(), session);
                }

                Log.Info("create new session ok" + session.ToString());

                return session;
            }

            Log.Info("not allowed : " + nodeId);

            return null;
        }

        public Session GetSession(string nodeId)
        {
            return nodeIdToSesseionDic[nodeId];
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

            Session session = nodeIdToSesseionDic[nodeId];
            if (session == null)
            {
                Log.Error("session is null ,nodeId:" + nodeId);
                return;
            }

            session.channel.WriteAndFlushAsync(message.content);
            Log.Info("WriteAndFlushAsync " + message.content);
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

            Session session = nodeIdToSesseionDic[nodeId];
            if (session == null)
            {
                Log.Error("session is null ,nodeId is " + nodeId);

                return;
            }

            // In this example, the command object is directly converted to a string and sent to the child device. In actual scenarios, encoding and decoding may be required.
            session.channel.WriteAndFlushAsync(JsonUtil.ConvertObjectToJsonString(command));

            // In this example, a command response is directly returned. It is more reasonable to send a respond after the gateway receives a response form the child device.
            GetClient().RespondCommand(requestId, new CommandRsp(0));
            Log.Info("WriteAndFlushAsync " + command);
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

            Session session = nodeIdToSesseionDic[nodeId];
            if (session == null)
            {
                Log.Error("session is null ,nodeId:" + nodeId);

                return;
            }

            // In this example, the command object is directly converted to a string and sent to the child device. In actual scenarios, encoding and decoding may be required.
            session.channel.WriteAndFlushAsync(JsonUtil.ConvertObjectToJsonString(propsSet));

            // In this example, a command response is directly returned. It is more reasonable to send a respond after the gateway receives a response form the child device.
            GetClient().RespondPropsSet(requestId, IotResult.SUCCESS);

            Log.Info("WriteAndFlushAsync " + propsSet);
        }
        
        public override void OnSubdevPropertiesGet(string requestId, PropsGet propsGet)
        {
            // It is not recommended that the platform directly read child device properties. Therefore, a failure message is returned.
            Log.Error("not supporte onSubdevPropertiesGet");
            GetClient().RespondPropsSet(requestId, IotResult.FAIL);
        }

        public override int OnDeleteSubDevices(SubDevicesInfo subDevicesInfo)
        {
            foreach (DeviceInfo subdevice in subDevicesInfo.devices)
            {
                if (nodeIdToSesseionDic.ContainsKey(subdevice.nodeId))
                {
                    Session session = nodeIdToSesseionDic[subdevice.nodeId];

                    if (session.channel != null)
                    {
                        session.channel.CloseAsync();

                        channelIdToSessionDic.Remove(session.channel.Id.AsLongText());

                        nodeIdToSesseionDic.Remove(session.nodeId);
                    }
                }
            }

            return base.OnDeleteSubDevices(subDevicesInfo);
        }
    }
}
