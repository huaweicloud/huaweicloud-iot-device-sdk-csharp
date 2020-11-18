using System;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using NLog;

namespace IoT.Gateway.Demo
{
    public class TcpDevice
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private string host;

        private int port;

        public TcpDevice(string host, int port)
        {
            this.host = host;
            this.port = port;

            Task.Run(async () => { await Run(); });
        }

        public async Task Run()
        {
            MultithreadEventLoopGroup group = new MultithreadEventLoopGroup();

            try
            {
                Bootstrap bootstrap = new Bootstrap()
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Handler(new SimpleClientInitializer());

                IChannel channel = await bootstrap.ConnectAsync(host, port);

                while (true)
                {
                    Log.Info("input string to send:");

                    string inputStr = Console.ReadLine();

                    if (string.IsNullOrEmpty(inputStr))
                    {
                        Log.Warn("input string is null.");

                        continue;
                    }

                    if (!channel.Active)
                    {
                        Log.Warn("tcp server is closed.");

                        break;
                    }
                    
                    await channel.WriteAndFlushAsync(inputStr);
                }
            }
            catch (Exception ex)
            {
                Log.Error("tcp device start error.");
            }
            finally
            {
                await group.ShutdownGracefullyAsync();
            }
        }

        public class SimpleClientHandler : SimpleChannelInboundHandler<string>
        {
            protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
            {
                Log.Info("channelRead0:" + msg);
            }
        }

        public class SimpleClientInitializer : ChannelInitializer<ISocketChannel>
        {
            protected override void InitChannel(ISocketChannel channel)
            {
                IChannelPipeline pipeline = channel.Pipeline;
                Log.Info("initChannel...");

                // pipeline.addLast("framer", new DelimiterBasedFrameDecoder(8192, Delimiters.lineDelimiter()));
                pipeline.AddLast("decoder", new StringDecoder());
                pipeline.AddLast("encoder", new StringEncoder());
                pipeline.AddLast("handler", new SimpleClientHandler());
            }
        }
    }
}
