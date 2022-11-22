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
using IoT.SDK.Device.Client;
using NLog;
using IoT.SDK.Bridge.Listener;
using IoT.SDK.Device.Service;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Utils;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Bridge.Request;
using IoT.SDK.Bridge.Handler;
using System.Threading.Tasks;
using MQTTnet;

namespace IoT.SDK.Bridge.Clent {
    public class BridgeClient : DeviceClient {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // bridgeClient相关请求topic

        private static readonly string BRIDGE_LOGIN = "$oc/bridges/{0}/devices/{1}/sys/login/request_id={2}";

        private static readonly string BRIDGE_LOGOUT = "$oc/bridges/{0}/devices/{1}/sys/logout/request_id={2}";

        private static readonly string BRIDGE_REPORT_PROPERTY = "$oc/bridges/{0}/devices/{1}/sys/properties/report";

        private static readonly string BRIDGE_RESET_DEVICE_SECRET = "$oc/bridges/{0}/devices/{1}/sys/reset_secret/request_id={2}";

        private static readonly string BRIDGE_REPORT_MESSAGE = "$oc/bridges/{0}/devices/{1}/sys/messages/up";

        private static readonly string BRIDEGE_EVENT = "$oc/bridges/{0}/devices/{1}/sys/events/up";

        private static readonly string BRIDGE_COMMAND_RESPONSE = "$oc/bridges/{0}/devices/{1}/sys/commands/response/request_id={2}";

        private static readonly string BRIDGE_PROP_SET_RESPONSE = "$oc/bridges/{0}/devices/{1}/sys/properties/set/response/request_id={2}";

        private static readonly string BRIDGE_PROP_GET_RESPONSE = "$oc/bridges/{0}/devices/{1}/sys/properties/get/response/request_id={2}";

        // bridgeClient相关的响应topic
        private static readonly string BRIDGE_PRE_HEAD_TOPIC = "$oc/bridges/{0}/devices/{1}/";

        private static readonly string MESSAGE_DOWN_TOPIC = "sys/messages/down";

        private static readonly string COMMAND_DOWN_TOPIC = "sys/commands/request_id";

        private static readonly string LOGIN_RESP_TOPIC = "sys/login/response/request_id";

        private static readonly string LOGOUT_RESP_TOPIC = "sys/logout/response/request_id";

        private static readonly string BRIDGE_RESET_DEVICE_SECRET_RESP = "sys/reset_secret/response/request_id";

        private static readonly string BRIDGE_DEVICE_DISCONNECT = "sys/disconnect";

        private static readonly string PROPERTY_SET_TOPIC = "sys/properties/set/request_id";

        private static readonly string PROPERTY_GET_TOPIC = "sys/properties/get/request_id";

        public static readonly string BRIDGE_TOPIC_KEYWORD = "$oc/bridges/";

        private string bridgeId;

        // bridge相关listener
        // 设置/获取网桥处理命令下发的监听器
        public BridgeCommandListener bridgeCommandListener { get; set; }

        // 设置/获取网桥处理消息下发的监听器
        public BridgeDeviceMessageListener bridgeDeviceMessageListener { get; set; }

        // 设置/获取网桥设备登录监听器
        public LoginListener loginListener { get; set; }

        // 设置/获取网桥设备登出监听器
        public LogoutListener logoutListener { get; set; }

        // 设置/获取网桥重置设备密钥监听器
        public ResetDeviceSecretListener resetDeviceSecretListener { get; set; }

        // 设置/获取网桥处理设备断链的监听器
        public BridgeDeviceDisConnListener bridgeDeviceDisConnListener { get; set; }

        // 设置/获取网桥属性查询/设置监听器
        public BridgePropertyListener bridgePropertyListener { get; set; }

        // 网关设备登录requestId对应缓存
        public RequestIdCache requestIdCache { get; }

        public BridgeClient(ClientConf clientConf, AbstractDevice device) : base(clientConf, device)
        {
            bridgeId = clientConf.DeviceId;
            requestIdCache = new RequestIdCache();
        }

        private PubMessage GenerateLoginMsg(string deviceId, string password, string requestId)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHH");
            // 对密码进行HmacSHA256加密
            string secret = string.Empty;
            if (!string.IsNullOrEmpty(password)) {
                secret = EncryptUtil.HmacSHA256(password, timestamp);
            }
            string topic = string.Format(BRIDGE_LOGIN, bridgeId, deviceId, requestId);
            string msg = "{\"sign_type\":" + JsonUtil.ConvertObjectToJsonString(1) + ",";
            msg += "\"timestamp\":" + JsonUtil.ConvertObjectToJsonString(timestamp) + ",";
            msg += "\"password\":" + JsonUtil.ConvertObjectToJsonString(secret) + "}";
            return new PubMessage(topic, msg);
        }

        public void LoginAsync(string deviceId, string password, string requestId)
        {
            SubscribeDeviceTopic(deviceId);
            PubMessage msg = GenerateLoginMsg(deviceId, password, requestId);
            Report(msg);
        }

        public void LogoutAsync(string deviceId, string requestId)
        {
            string topic = string.Format(BRIDGE_LOGOUT, this.bridgeId, deviceId, requestId);
            PubMessage msg = new PubMessage(topic, "");
            Report(msg);
        }

        public int LoginSync(string deviceId, string password, int millisecondTimeout)
        {
            SubscribeDeviceTopic(deviceId);
            string requestId = Guid.NewGuid().ToString();
            TaskCompletionSource<int> future = new TaskCompletionSource<int>();
            PubMessage msg = GenerateLoginMsg(deviceId, password, requestId);
            return GetSyncResult(millisecondTimeout, requestId, future, msg);
        }

        public int LogoutSync(string deviceId, int millisecondTimeout)
        {
            string requestId = Guid.NewGuid().ToString();
            TaskCompletionSource<int> future = new TaskCompletionSource<int>();
            string topic = string.Format(BRIDGE_LOGOUT, this.bridgeId, deviceId, requestId);
            PubMessage msg = new PubMessage(topic, "");
            Report(msg);
            
            return GetSyncResult(millisecondTimeout, requestId, future, msg);
        }

        private int GetSyncResult(int millisecondTimeout, string requestId, TaskCompletionSource<int> future, PubMessage msg)
        {
            requestIdCache.SetRequestId2Cache(requestId, future);
            Report(msg);

            if (future.Task.Wait(TimeSpan.FromMilliseconds(millisecondTimeout))) {
                return future.Task.Result;
            } else {
                return -1;
            }
        }

        public void ReportProperties(string deviceId, List<ServiceProperty> properties)
        {
            string topic = string.Format(BRIDGE_REPORT_PROPERTY, bridgeId, deviceId);
            Report(new PubMessage(topic, properties));
        }

        public void ResetSecret(string deviceId, string requestId, DeviceSecret deviceSecret)
        {
            string topic = string.Format(BRIDGE_RESET_DEVICE_SECRET, bridgeId, deviceId, requestId);
            string payLoad = JsonUtil.ConvertObjectToJsonString(deviceSecret);
            Report(new PubMessage(topic, payLoad));
        }

        public void ReportDeviceMessage(string deviceId, DeviceMessage deviceMessage)
        {
            string topic = string.Format(BRIDGE_REPORT_MESSAGE, bridgeId, deviceId);
            string payLoad = JsonUtil.ConvertObjectToJsonString(deviceMessage);
            Report(new PubMessage(topic, payLoad));
        }

        public override void ReportEvent(string deviceId, DeviceEvent evnt)
        {
            string topic = string.Format(BRIDEGE_EVENT, bridgeId, deviceId);
            DeviceEvents events = new DeviceEvents();
            events.deviceId = deviceId;
            List<DeviceEvent> services = new List<DeviceEvent>();
            services.Add(evnt);
            events.services = services;
            Report(new PubMessage(topic, events));
        }

        public void RespondCommand(string deviceId, string requestId, CommandRsp commandRsp)
        {
            string topic = string.Format(BRIDGE_COMMAND_RESPONSE, bridgeId, deviceId, requestId);
            string payload = JsonUtil.ConvertObjectToJsonString(commandRsp);
            Report(new PubMessage(topic, payload));
        }

        public void RespondPropsGet(string deviceId, string requestId, List<ServiceProperty> services)
        {
            string topic = string.Format(BRIDGE_PROP_GET_RESPONSE, this.bridgeId, deviceId, requestId);
            DeviceProperties deviceProperties = new DeviceProperties();
            deviceProperties.services = services;
            Report(new PubMessage(topic, deviceProperties));
        }

        public void RespondPropsSet(string deviceId, string requestId, IotResult iotResult)
        {
            string topic = string.Format(BRIDGE_PROP_SET_RESPONSE, bridgeId, deviceId, requestId);
            Report(new PubMessage(topic, JsonUtil.ConvertObjectToJsonString(iotResult)));
        }

        private void SubscribeDeviceTopic(string deviceId)
        {
            List<MqttTopicFilter> listTopic = new List<MqttTopicFilter>();
            string topicMsgDown = string.Format(BRIDGE_PRE_HEAD_TOPIC, bridgeId, deviceId) + MESSAGE_DOWN_TOPIC;
            SubscribeCompleteTopic(topicMsgDown, MESSAGE_DOWN_TOPIC, new BridgeMessageHandler(this));

            string topicCommand = string.Format(BRIDGE_PRE_HEAD_TOPIC, bridgeId, deviceId) + COMMAND_DOWN_TOPIC;
            SubscribeCompleteTopic(topicCommand, COMMAND_DOWN_TOPIC, new BridgeCommandHandler(this));

            var topicLogin = string.Format(BRIDGE_PRE_HEAD_TOPIC, bridgeId, deviceId) + LOGIN_RESP_TOPIC;
            SubscribeCompleteTopic(topicLogin, LOGIN_RESP_TOPIC, new DeviceLoginHandler(this));

            var topicLogout = string.Format(BRIDGE_PRE_HEAD_TOPIC, bridgeId, deviceId) + LOGOUT_RESP_TOPIC;
            SubscribeCompleteTopic(topicLogout, LOGOUT_RESP_TOPIC, new DeviceLogoutHandler(this));

            var topicRstSecret = string.Format(BRIDGE_PRE_HEAD_TOPIC, bridgeId, deviceId) + BRIDGE_RESET_DEVICE_SECRET_RESP;
            SubscribeCompleteTopic(topicRstSecret, BRIDGE_RESET_DEVICE_SECRET_RESP, new SecretResetHandler(this));

            var topicDisConnect = string.Format(BRIDGE_PRE_HEAD_TOPIC, bridgeId, deviceId) + BRIDGE_DEVICE_DISCONNECT;
            SubscribeCompleteTopic(topicDisConnect, BRIDGE_DEVICE_DISCONNECT, new DeviceDisConnHandler(this));

            var topicPropertySet = string.Format(BRIDGE_PRE_HEAD_TOPIC, bridgeId, deviceId) + PROPERTY_SET_TOPIC;
            SubscribeCompleteTopic(topicPropertySet, PROPERTY_SET_TOPIC, new BridgePropertySetHandler(this));

            var topicPropertyGet = string.Format(BRIDGE_PRE_HEAD_TOPIC, bridgeId, deviceId) + PROPERTY_GET_TOPIC;
            SubscribeCompleteTopic(topicPropertyGet, PROPERTY_GET_TOPIC, new BridgePropertyGetHandler(this));

        }

    }
}

        
