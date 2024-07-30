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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using NLog;

namespace IoT.TCP.Device.Test
{
    public class TcpDevice
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly string host;

        private readonly int port;

        private readonly IEnumerable<string> deviceArgs;


        public TcpDevice(string host, int port, IEnumerable<string> deviceArgs)
        {
            this.host = host;
            this.port = port;
            this.deviceArgs = deviceArgs;
        }

        public async Task<bool> Run()
        {
            SingleThreadEventLoop group = new SingleThreadEventLoop();
            try
            {
                Bootstrap bootstrap = new Bootstrap()
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Handler(new SimpleClientInitializer());

                IChannel channel = await bootstrap.ConnectAsync(host, port);
                await channel.WriteAndFlushAsync("login:" + string.Join(',', deviceArgs) + '\n');
                LOG.Info("send login message finished");

                while (true)
                {
                    if (!channel.Active)
                    {
                        LOG.Warn("tcp server is closed.");
                        break;
                    }

                    LOG.Info("input string to send:");
                    string inputStr = Console.ReadLine();
                    if (string.IsNullOrEmpty(inputStr))
                    {
                        LOG.Warn("input string is null.");
                        continue;
                    }

                    if (inputStr.Equals("q"))
                    {
                        LOG.Info("quit now");
                        await channel.CloseAsync();
                        return true;
                    }

                    await channel.WriteAndFlushAsync(inputStr + '\n');
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "tcp device start error");
            }
            finally
            {
                group.ShutdownGracefullyAsync().Wait(TimeSpan.FromSeconds(1));
            }

            return false;
        }

        private class SimpleClientHandler : SimpleChannelInboundHandler<string>
        {
            protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
            {
                LOG.Info("server:" + msg);
            }
        }

        private class SimpleClientInitializer : ChannelInitializer<ISocketChannel>
        {
            protected override void InitChannel(ISocketChannel channel)
            {
                IChannelPipeline pipeline = channel.Pipeline;
                LOG.Info("initChannel...");

                pipeline.AddLast("framer", new LineBasedFrameDecoder(8192));
                pipeline.AddLast("decoder", new StringDecoder());
                pipeline.AddLast("encoder", new StringEncoder());
                pipeline.AddLast("handler", new SimpleClientHandler());
            }
        }
    }
}