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

            ////Add sub device on device side.
            ////simpleGateway.ReportAddSubDevice(subDeviceInfoList);

            List<string> deviceIds = new List<string>();

            deviceIds.Add("5eb4cd4049a5ab087d7d4861_test_sub");

            ////Delete sub device on device side
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

                // Creates a session if the message is the first message.
                Session session = simpleGateway.GetSessionByChannel(incoming.Id.AsLongText());
                if (session == null)
                {
                    string nodeId = msg;
                    session = simpleGateway.CreateSession(nodeId, incoming);

                    // Rejects the connection is the session fails to create.
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
                    // When receiving upstream data from a child device, the gateway can report the data to the IoT platform as a message or property.
                    // This example shows both data reporting types. In practice, select either one.

                    // Calls reportSubDeviceMessage to report a message.
                    DeviceMessage deviceMessage = new DeviceMessage(msg);
                    deviceMessage.deviceId = session.deviceId;
                    simpleGateway.ReportSubDeviceMessage(deviceMessage);

                    // Calls reportSubDeviceProperties to report properties. The serviceId and field names of the properties must be the same as those defined in the product model of the child device.
                    ServiceProperty serviceProperty = new ServiceProperty();
                    serviceProperty.serviceId = "smokeDetector";
                    Dictionary<string, object> props = new Dictionary<string, object>();

                    // In this example, the property values are hardcoded. In practice, they are assembled using the values reported by the child device.
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
