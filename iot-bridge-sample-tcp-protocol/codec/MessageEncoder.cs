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
using IoT.SDK.Device.Utils;
using System.Threading.Tasks;
using NLog;

namespace IoT.Bridge.Sample.Tcp.Codec {
    class MessageEncoder : ChannelHandlerAdapter {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override Task WriteAsync(IChannelHandlerContext ctx, object msg)
        {
            Log.Info("MessageEncoder msg={}", JsonUtil.ConvertObjectToJsonString(msg));
            BaseMessage baseMessage = (BaseMessage)msg;
            MsgHeader msgHeader = baseMessage.msgHeader;

            StringBuilder stringBuilder = new StringBuilder();
            EncodeHeader(msgHeader, stringBuilder);

            // 根据消息类型编码消息
            switch (msgHeader.msgType) {
                case Constants.MSG_TYPE_DEVICE_LOGIN:
                case Constants.MSG_TYPE_REPORT_LOCATION_INFO:
                    stringBuilder.Append(((CommonResponse)msg).resultCode);
                    break;
                case Constants.MSG_TYPE_FREQUENCY_LOCATION_SET:
                    stringBuilder.Append(((DeviceLocationFrequencySet)msg).period);
                    break;
                default:
                    Log.Warn("invalid msgType");
                    return new Task(()=> { });
            }

            // 添加结束符
            stringBuilder.Append(Constants.MESSAGE_END_DELIMITER);

            return ctx.WriteAsync(stringBuilder.ToString());
        }

        private void EncodeHeader(MsgHeader msgHeader, StringBuilder sb)
        {
            sb.Append(Constants.MESSAGE_START_DELIMITER)
              .Append(msgHeader.deviceId)
              .Append(Constants.HEADER_PARS_DELIMITER)
              .Append(msgHeader.flowNo)
              .Append(Constants.HEADER_PARS_DELIMITER)
              .Append(msgHeader.msgType)
              .Append(Constants.HEADER_PARS_DELIMITER)
              .Append(msgHeader.direct)
              .Append(Constants.HEADER_PARS_DELIMITER);
        }
    }
}
