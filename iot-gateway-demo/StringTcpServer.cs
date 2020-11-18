using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Gateway.Requests;
using NLog;

namespace IoT.Gateway.Demo
{
    public class StringTcpServer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static SimpleGateway simpleGateway;
        
        private int port = 8080;

        public StringTcpServer(string serverUri, int port, string deviceId, string deviceSecret)
        {
            simpleGateway = new SimpleGateway(new SubDevicesFilePersistence(), serverUri, port, deviceId, deviceSecret);

            if (simpleGateway.Init() != 0)
            {
                return;
            }
            
            List<DeviceInfo> subDeviceInfoList = new List<DeviceInfo>();

            DeviceInfo deviceInfo = new DeviceInfo();

            deviceInfo.nodeId = "test_sub";

            deviceInfo.productId = "5eb4cd4049a5ab087d7d4861";

            subDeviceInfoList.Add(deviceInfo);

            ////设备侧添加子设备
            ////simpleGateway.ReportAddSubDevice(subDeviceInfoList);

            List<string> deviceIds = new List<string>();

            deviceIds.Add("5eb4cd4049a5ab087d7d4861_test_sub");

            ////设备侧删除子设备
            ////simpleGateway.ReportDeleteSubDevice(deviceIds);

            Task.Run(async () => { await Run(); });
        }

        public async Task Run()
        {
            MultithreadEventLoopGroup bossGroup = new MultithreadEventLoopGroup();
            MultithreadEventLoopGroup workerGroup = new MultithreadEventLoopGroup();

            try
            {
                ServerBootstrap b = new ServerBootstrap();
                b.Group(bossGroup, workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        
                        pipeline.AddLast("decoder", new StringDecoder());
                        pipeline.AddLast("encoder", new StringEncoder());
                        pipeline.AddLast("handler", new StringHandler());

                        Log.Info("initChannel:" + channel.RemoteAddress);
                    }))
                    .Option(ChannelOption.SoBacklog, 128)
                    .ChildOption(ChannelOption.SoKeepalive, true);

                Log.Info("tcp server start......");

                IChannel f = await b.BindAsync(port);

                await f.CloseCompletion;
            }
            catch (Exception ex)
            {
                Log.Error("tcp server start error.");
            }
            finally
            {
                await workerGroup.ShutdownGracefullyAsync();
                await bossGroup.ShutdownGracefullyAsync();

                Log.Info("tcp server close");
            }
        }

        public class StringHandler : SimpleChannelInboundHandler<string>
        {
            protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
            {
                IChannel incoming = ctx.Channel;
                Log.Info("channelRead0" + incoming.RemoteAddress + " msg :" + msg);

                // 如果是首条消息,创建session
                Session session = simpleGateway.GetSessionByChannel(incoming.Id.AsLongText());
                if (session == null)
                {
                    string nodeId = msg;
                    session = simpleGateway.CreateSession(nodeId, incoming);

                    // 创建会话失败，拒绝连接
                    if (session == null)
                    {
                        Log.Info("close channel");
                        ctx.CloseAsync().Wait();
                    }
                    else
                    {
                        Log.Info(session.deviceId + " ready to go online.");
                        simpleGateway.ReportSubDeviceStatus(session.deviceId, "ONLINE");
                    }
                }
                else
                {
                    // 网关收到子设备上行数据时，可以以消息或者属性上报转发到平台。
                    // 实际使用时根据需要选择一种即可，这里为了演示，两种类型都转发一遍

                    // 上报消息用reportSubDeviceMessage
                    DeviceMessage deviceMessage = new DeviceMessage(msg);
                    deviceMessage.deviceId = session.deviceId;
                    simpleGateway.ReportSubDeviceMessage(deviceMessage);

                    // 报属性则调用reportSubDeviceProperties，属性的serviceId和字段名要和子设备的产品模型保持一致
                    ServiceProperty serviceProperty = new ServiceProperty();
                    serviceProperty.serviceId = "smokeDetector";
                    Dictionary<string, object> props = new Dictionary<string, object>();

                    // 属性值暂且写死，实际中应该根据子设备上报的进行组装
                    props.Add("alarm", 1);
                    props.Add("temperature", 2);
                    serviceProperty.properties = props;

                    List<ServiceProperty> services = new List<ServiceProperty>();
                    services.Add(serviceProperty);
                    simpleGateway.ReportSubDeviceProperties(session.deviceId, services);
                }
            }
        }
    }
}
