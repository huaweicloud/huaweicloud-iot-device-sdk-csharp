/*Copyright (c) <2020>, <Huawei Technologies Co., Ltd>
 * All rights reserved.
 * &Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice, this list of
 * conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list
 * of conditions and the following disclaimer in the documentation and/or other materials
 * provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used
 * to endorse or promote products derived from this software without specific prior written permission.
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 *
 * */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoT.SDK.Device.Config
{
    public static class CommonTopic
    {
        public static readonly string TOPIC_PROPERTIES_REPORT = "$oc/devices/{0}/sys/properties/report";
        public static readonly string TOPIC_MESSAGES_UP = "$oc/devices/{0}/sys/messages/up";
        public static readonly string TOPIC_COMMANDS_RESPONSE = "$oc/devices/{0}/sys/commands/response/request_id";
        public static readonly string TOPIC_SYS_COMMAND = "$oc/devices/{0}/sys/commands/#";
        public static readonly string TOPIC_SYS_SHADOW_GET = "$oc/devices/{0}/sys/shadow/get/request_id";
        public static readonly string TOPIC_SYS_SHADOW_GET_RESPONSE = "$oc/devices/{0}/sys/shadow/get/response/#";
        public static readonly string TOPIC_SYS_MESSAGES_DOWN = "$oc/devices/{0}/sys/messages/down";
        public static readonly string PRE_TOPIC = "$oc/devices/{0}/user/";
        public static readonly string TOPIC_SYS_PROPERTIES_SET = "$oc/devices/{0}/sys/properties/set/#";
        public static readonly string TOPIC_SYS_PROPERTIES_SET_RESPONSE = "$oc/devices/{0}/sys/properties/set/response/request_id";
        public static readonly string TOPIC_SYS_PROPERTIES_GET = "$oc/devices/{0}/sys/properties/get/#";
        public static readonly string TOPIC_SYS_PROPERTIES_GET_RESPONSE = "$oc/devices/{0}/sys/properties/get/response/request_id";
        public static readonly string TOPIC_SYS_EVENTS_DOWN = "$oc/devices/{0}/sys/events/down";
        public static readonly string TOPIC_SYS_EVENTS_UP = "$oc/devices/{0}/sys/events/up";
    }
}
