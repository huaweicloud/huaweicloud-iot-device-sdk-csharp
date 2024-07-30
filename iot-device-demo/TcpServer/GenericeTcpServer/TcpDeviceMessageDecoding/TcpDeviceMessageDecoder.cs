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
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using IoT.Bridge.Sample.Tcp.Session;
using IoT.Device.Demo.HubDevice;
using IoT.SDK.Device.Client.Requests;
using Newtonsoft.Json;
using NLog;

namespace IoT.Bridge.Sample.Tcp.TcpDeviceMessageDecoding
{
    public class TcpDeviceMessageDecoder : MessageToMessageDecoder<string>
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, Func<string, TcpDeviceMessage>> opMaps;
        private readonly ISessionManager sessionManager;

        public TcpDeviceMessageDecoder(ISessionManager sessionManager)
        {
            opMaps = new Dictionary<string, Func<string, TcpDeviceMessage>>
            {
                { "message", DecodeDeviceMessage },
                { "properties", DecodeReportProperties },
                { "login", DecodeLoginMessage },
                { "cmdresp", DecodeCommandResp }
            };
            this.sessionManager = sessionManager;
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (NettyUtils.GetSessionIdFromChannel(context.Channel) != null)
            {
                LOG.Debug("decode message, remote address:{}, message:{}",
                    context.Channel.RemoteAddress, message);
            }
            else
            {
                LOG.Warn("device on this channel haven't login, remote  address:{}, message:{}",
                    context.Channel.RemoteAddress, message);
            }

            base.ChannelRead(context, message);
        }

        protected override void Decode(IChannelHandlerContext context, string message, List<object> output)
        {
            var msgFragments = message.Trim().Split(new[] { ':' }, 2);
            if (msgFragments.Length < 2)
            {
                LOG.Error("invalid message format");
            }

            var opType = msgFragments[0].Trim();
            if (!opMaps.TryGetValue(opType, out var opCallback))
            {
                LOG.Error("unknown operation type:{}", opType);
                return;
            }

            if (opCallback == null) return;
            var o = opCallback(msgFragments[1]);
            switch (o)
            {
                case null:
                    return;
                case TcpDeviceLoginMessage _:
                {
                    NettyUtils.LinkSessionIdToChannel(context.Channel, o.DeviceOrNodeId);
                    var session =
                        sessionManager.TryCreateSession(o.DeviceOrNodeId, context.Channel, out var isNewSession);
                    if (session == null)
                    {
                        LOG.Info("create session for device failed.");
                        context.CloseAsync();
                    }
                    else if (isNewSession == false)
                    {
                        LOG.Info("node id {} already have corresponding session with id {}",
                            session.nodeId,
                            session.deviceId);
                    }
                    else
                    {
                        LOG.Info(" {} ready to go online.", session.deviceId);
                    }

                    break;
                }
                default:
                    o.DeviceOrNodeId = NettyUtils.GetSessionIdFromChannel(context.Channel);
                    break;
            }

            output.Add(o);
        }

        private TcpDeviceMessage DecodeDeviceMessage(string operand)
        {
            // When receiving upstream data from a child device, the gateway can report the data to the IoT platform as a message or property.
            // This example shows both data reporting types. In practice, select either one.
            // Calls reportSubDeviceMessage to report a message.
            return new TcpDeviceDeviceMessage
            {
                Message = operand.TrimStart()
            };
        }

        private TcpDeviceMessage DecodeReportProperties(string operand)
        {
            var fragments = operand.Split(new[] { ',' }, 2);
            if (fragments.Length < 2)
            {
                LOG.Error("invalid properties report format");
            }

            var services = new List<ServiceProperty>
            {
                new ServiceProperty
                {
                    serviceId = fragments[0].Trim(),
                    properties = JsonConvert.DeserializeObject<Dictionary<string, Object>>(fragments[1])
                }
            };

            return new TcpDevicePropertiesReportMessage
            {
                Services = services
            };
        }


        private TcpDeviceMessage DecodeLoginMessage(string operand)
        {
            var fragments = operand.Split(new[] { ',' }, 2);
            if (fragments.Length < 1)
            {
                LOG.Error("invalid login report format");
                return null;
            }


            return new TcpDeviceLoginMessage
            {
                DeviceOrNodeId = fragments[0],
                Secret = fragments.Length > 1 ? fragments[1] : null // null for gateway
            };
        }

        private TcpDeviceMessage DecodeCommandResp(string operand)
        {
            var fragments = operand.Split(new[] { ',' }, 2);
            if (fragments.Length < 2)
            {
                LOG.Error("invalid command response report format");
                return null;
            }


            return new TcpDeviceCommandResponseMessage
            {
                RequestId = fragments[0].Trim(),
                Response = new CommandRsp(int.Parse(fragments[1]))
            };
        }


        public override void ChannelInactive(IChannelHandlerContext context)
        {
            context.FireChannelRead(new TcpDeviceLogoutMessage
            {
                DeviceOrNodeId = NettyUtils.GetSessionIdFromChannel(context.Channel)
            });
            sessionManager.RemoveSession(NettyUtils.GetSessionIdFromChannel(context.Channel), out _);
        }
    }
}