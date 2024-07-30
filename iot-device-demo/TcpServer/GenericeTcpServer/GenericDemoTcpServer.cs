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
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using IoT.Bridge.Sample.Tcp.TcpDeviceMessageDecoding;
using IoT.Device.Demo.HubDevice;
using NLog;

namespace IoT.Device.Demo.Gateway
{
    public class GenericDemoTcpServer
    {
        private const int MAX_FRAME_LENGTH = 1024;
        private const int DEFAULT_BUF_VALUE = 1024 * 1024;
        private const int DEFAULT_IDLE_TIME = 300;
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();


        public static async Task CreateServer(int port, ISessionManager sessionManager, Dictionary<string, Func<IChannelHandler>> handlers)
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
                        pipeline.AddFirst("readTimeoutHandler", new ReadTimeoutHandler(DEFAULT_IDLE_TIME));
                        pipeline.AddLast("frameDecoder", new LineBasedFrameDecoder(MAX_FRAME_LENGTH));
                        pipeline.AddLast("decoder", new StringDecoder());
                        pipeline.AddLast("TcpDeviceMessageDecoder", new TcpDeviceMessageDecoder(sessionManager));
                        pipeline.AddLast("encoder", new StringEncoder());
                        foreach (var (key, value) in handlers)
                        {
                            pipeline.AddLast(key, value());
                        }
                        LOG.Info("initChannel: {}", channel.RemoteAddress);
                    }))
                    .Option(ChannelOption.SoBacklog, 128)
                    .Option(ChannelOption.TcpNodelay, true)
                    .Option(ChannelOption.SoRcvbuf, DEFAULT_BUF_VALUE)
                    .ChildOption(ChannelOption.SoKeepalive, true)
                    .ChildOption(ChannelOption.SoSndbuf, DEFAULT_BUF_VALUE);

                LOG.Info("tcp server start......");

                IChannel f = await b.BindAsync(port);

                await f.CloseCompletion;
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "tcp server start failed");
            }
            finally
            {
                await workerGroup.ShutdownGracefullyAsync();
                await bossGroup.ShutdownGracefullyAsync();

                LOG.Info("tcp server close");
            }
        }
    }
}