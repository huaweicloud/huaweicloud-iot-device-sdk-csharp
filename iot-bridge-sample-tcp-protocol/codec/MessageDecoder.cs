/*
 * Copyright (c) 2022-2022 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Text;
using DotNetty.Transport.Channels;
using IoT.Bridge.Sample.Tcp.Constant;
using IoT.Bridge.Sample.Tcp.Dto;
using NLog;

namespace IoT.Bridge.Sample.Tcp.Codec {
    class MessageDecoder : SimpleChannelInboundHandler<string> {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override void ChannelRead(IChannelHandlerContext ctx, object msg)
        {
            Log.Info("MessageDecoder msg={0}", msg);
            if (!CheckInComingMsg(msg)) {
                return;
            }
            int startIndex = ((string)msg).IndexOf(Constants.MESSAGE_START_DELIMITER);
            if (startIndex < 0) {
                return;
            }

            BaseMessage message = DecodeMessage(((string)msg).Substring(startIndex + 1));
            if (message == null) {
                Log.Warn("decode message failed");
                return;
            }
            ctx.FireChannelRead(message);
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, string msg) { }
        private bool CheckInComingMsg(object msg) { return msg is string && ((string)msg).Length != 0; }

        private BaseMessage DecodeMessage(string message)
        {
            MsgHeader header = DecodeHeader(message);
            if (header == null) {
                return null;
            }
            BaseMessage baseMessage = DecodeBody(header, message.Substring(message.LastIndexOf(",") + 1));
            if (baseMessage == null) {
                return null;
            }
            baseMessage.msgHeader = header;
            return baseMessage;
        }

        private MsgHeader DecodeHeader(string message)
        {
            string[] splits = message.Split(Constants.HEADER_PARS_DELIMITER);
            if (splits.Length <= 4) { // tcp报文标准格式为[867082058798193,0,DEVICE_LOGIN,3,12345678]，以","分割，故split之后其数组个数应大于4
                return null;
            }

            MsgHeader msgHeader = new MsgHeader();
            msgHeader.deviceId = splits[0];
            msgHeader.flowNo = splits[1];
            msgHeader.msgType = splits[2];
            msgHeader.direct = int.Parse(splits[3]);
            return msgHeader;
        }

        private BaseMessage DecodeBody(MsgHeader header, string body)
        {
            switch (header.msgType) {
                case Constants.MSG_TYPE_DEVICE_LOGIN:
                    return DecodeLoginMessage(body);

                case Constants.MSG_TYPE_REPORT_LOCATION_INFO:
                    return DecodeLocationMessage(body);

                case Constants.MSG_TYPE_FREQUENCY_LOCATION_SET:
                    return DecodeLocationSetMessage(body);

                default:
                    Log.Warn("invalid msgType");
                    return null;
            }
        }

        private BaseMessage DecodeLoginMessage(string body)
        {
            DeviceLoginMessage loginMessage = new DeviceLoginMessage();
            loginMessage.secret = body;
            return loginMessage;
        }

        private BaseMessage DecodeLocationMessage(string body)
        {
            string[] splits = body.Split(Constants.BODY_PARS_DELIMITER);
            DeviceLocationMessage deviceLocationMessage = new DeviceLocationMessage();
            deviceLocationMessage.longitude = splits[0];
            deviceLocationMessage.latitude = splits[1];
            return deviceLocationMessage;
        }

        private BaseMessage DecodeLocationSetMessage(string body)
        {
            CommonResponse commonResponse = new CommonResponse();
            commonResponse.resultCode = int.Parse(body);
            return commonResponse;
        }
    }
}
