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
    /// 此例子用来演示如何使用云网关来实现TCP协议设备接入。网关和平台只建立一个MQTT连接，使用网关的身份
    /// 和平台进行通讯。本例子TCP server传输简单的字符串，并且首条消息会发送设备标识来鉴权。用户可以自行扩展StringTcpServer类
    /// 来实现更复杂的TCP server。
    /// </summary>
    public class SimpleGateway : AbstractGateway
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private Dictionary<string, Session> nodeIdToSesseionDic; // 保存设备标识码和session的映射

        private Dictionary<string, Session> channelIdToSessionDic; // 保存channelId和session的映射

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
            // 北向已经添加了此设备
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

            // 这里我们直接把command对象转成string发给子设备，实际场景中可能需要进行一定的编解码转换
            session.channel.WriteAndFlushAsync(JsonUtil.ConvertObjectToJsonString(command));

            // 为了简化处理，我们在这里直接回命令响应。更合理做法是在子设备处理完后再回响应
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

            // 这里我们直接把对象转成string发给子设备，实际场景中可能需要进行一定的编解码转换
            session.channel.WriteAndFlushAsync(JsonUtil.ConvertObjectToJsonString(propsSet));

            // 为了简化处理，我们在这里直接回响应。更合理做法是在子设备处理完后再回响应
            GetClient().RespondPropsSet(requestId, IotResult.SUCCESS);

            Log.Info("WriteAndFlushAsync " + propsSet);
        }
        
        public override void OnSubdevPropertiesGet(string requestId, PropsGet propsGet)
        {
            // 不建议平台直接读子设备的属性，这里直接返回失败
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
