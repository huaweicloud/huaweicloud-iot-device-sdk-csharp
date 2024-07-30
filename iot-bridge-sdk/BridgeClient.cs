/*
 * Copyright (c) 2022-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Linq;
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
using Newtonsoft.Json;

namespace IoT.SDK.Bridge.Clent
{
    public class BridgeClient : DeviceClient
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, BridgeHandler> dict;


        private const string BRIDGE_TOPIC_KEYWORD = "$oc/bridges/";

        private const string TOPIC_BRIDGE_PREFIX = "$oc/bridges/{0}/devices/{1}/sys/";
        // bridgeClient相关请求topic

        private const string TOPIC_DEVICE_LOGIN = TOPIC_BRIDGE_PREFIX + "login/request_id={2}";

        private const string TOPIC_DEVICE_LOGOUT = TOPIC_BRIDGE_PREFIX + "logout/request_id={2}";

        private const string TOPIC_REPORT_PROPERTY = TOPIC_BRIDGE_PREFIX + "properties/report";

        private const string TOPIC_REPORT_GATEWAY_PROPERTIES =
            TOPIC_BRIDGE_PREFIX + "gateway/sub_devices/properties/report";

        private const string TOPIC_RESET_DEVICE_SECRET = TOPIC_BRIDGE_PREFIX + "reset_secret/request_id={2}";

        private const string TOPIC_REPORT_MESSAGE = TOPIC_BRIDGE_PREFIX + "messages/up";

        private const string TOPIC_REPORT_EVENT = TOPIC_BRIDGE_PREFIX + "events/up";

        private const string TOPIC_COMMAND_RESPONSE = TOPIC_BRIDGE_PREFIX + "commands/response/request_id={2}";

        private const string TOPIC_PROP_SET_RESPONSE = TOPIC_BRIDGE_PREFIX + "properties/set/response/request_id={2}";

        private const string TOPIC_PROP_GET_RESPONSE = TOPIC_BRIDGE_PREFIX + "properties/get/response/request_id={2}";

        private const string TOPIC_SHADOW_GET_REQ = TOPIC_BRIDGE_PREFIX + "shadow/get/request_id={2}";

        // bridgeClient相关的响应topic   

        private const string TOPIC_MESSAGE_DOWN = "sys/messages/down";

        private const string TOPIC_COMMAND_DOWN = "sys/commands/request_id";

        private const string TOPIC_LOGIN_RESP = "sys/login/response/request_id";

        private const string TOPIC_LOGOUT_RESP = "sys/logout/response/request_id";

        private const string TOPIC_RESET_DEVICE_SECRET_RESP = "sys/reset_secret/response/request_id";

        private const string TOPIC_DEVICE_DISCONNECT = "sys/disconnect";

        private const string TOPIC_PROPERTY_SET = "sys/properties/set/request_id";

        private const string TOPIC_PROPERTY_GET = "sys/properties/get/request_id";
        private const string TOPIC_EVENT_DOWN = "sys/events/down";

        private const string TOPIC_SHADOW_GET_RESP = "shadow/get/response/request_id";


        private readonly string bridgeId;

        // bridge相关listener
        // 设置/获取网桥处理命令下发的监听器
        public BridgeCommandListener bridgeCommandListener { get; set; }

        public BridgeRawDeviceMessageListener BridgeRawDeviceMessageListener { get; set; }

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

        // 设置/事件下发/监听器
        public BridgeEventListener BridgeEventListener { get; set; }

        // 设置/影子下发/监听器
        public BridgeShadowListener BridgeShadowListener { get; set; }

        // 网关设备登录requestId对应缓存
        public RequestIdCache<int> requestIdCache { get; }

        public BridgeClient(ClientConf clientConf, AbstractDevice device) : base(clientConf, device)
        {
            bridgeId = clientConf.DeviceId;
            requestIdCache = new RequestIdCache<int>();

            dict = new Dictionary<string, BridgeHandler>
            {
                { TOPIC_MESSAGE_DOWN, new BridgeMessageHandler() },
                { TOPIC_COMMAND_DOWN, new BridgeCommandHandler() },
                { TOPIC_LOGIN_RESP, new DeviceLoginHandler() },
                { TOPIC_LOGOUT_RESP, new DeviceLogoutHandler() },
                { TOPIC_RESET_DEVICE_SECRET_RESP, new SecretResetHandler() },
                { TOPIC_DEVICE_DISCONNECT, new DeviceDisConnHandler() },
                { TOPIC_PROPERTY_SET, new BridgePropertySetHandler() },
                { TOPIC_PROPERTY_GET, new BridgePropertyGetHandler() },
                { TOPIC_EVENT_DOWN, new BridgeEventHandler() },
                { TOPIC_SHADOW_GET_RESP, new BridgeShadowHandler() }
            };
        }

        private PubMessage GenerateLoginMsg(string deviceId, string password, string requestId)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHH");
            // 对密码进行HmacSHA256加密
            string secret = string.Empty;
            if (!string.IsNullOrEmpty(password))
            {
                secret = EncryptUtil.HmacSHA256(password, timestamp);
            }

            string topic = string.Format(TOPIC_DEVICE_LOGIN, bridgeId, deviceId, requestId);
            string msg = "{\"sign_type\":" + JsonUtil.ConvertObjectToJsonString(1) + ",";
            msg += "\"timestamp\":" + JsonUtil.ConvertObjectToJsonString(timestamp) + ",";
            msg += "\"password\":" + JsonUtil.ConvertObjectToJsonString(secret) + "}";
            return new PubMessage(topic, msg);
        }

        public string LoginAsync(string deviceId, string password, string requestId = null)
        {
            requestId ??= Guid.NewGuid().ToString();
            PubMessage msg = GenerateLoginMsg(deviceId, password, requestId);
            Report(msg);
            return requestId;
        }

        public int LoginSync(string deviceId, string password, int millisecondTimeout)
        {
            string requestId = Guid.NewGuid().ToString();
            PubMessage msg = GenerateLoginMsg(deviceId, password, requestId);
            TaskCompletionSource<int> future = new TaskCompletionSource<int>();
            return GetSyncResult(millisecondTimeout, requestId, future, msg);
        }

        public string LogoutAsync(string deviceId, string requestId = null)
        {
            requestId ??= Guid.NewGuid().ToString();
            string topic = string.Format(TOPIC_DEVICE_LOGOUT, this.bridgeId, deviceId, requestId);
            PubMessage msg = new PubMessage(topic, "");
            Report(msg);
            return requestId;
        }


        public int LogoutSync(string deviceId, int millisecondTimeout)
        {
            string requestId = Guid.NewGuid().ToString();
            string topic = string.Format(TOPIC_DEVICE_LOGOUT, this.bridgeId, deviceId, requestId);
            PubMessage msg = new PubMessage(topic, "");

            TaskCompletionSource<int> future = new TaskCompletionSource<int>();
            return GetSyncResult(millisecondTimeout, requestId, future, msg);
        }

        private int GetSyncResult(int millisecondTimeout, string requestId, TaskCompletionSource<int> future,
            PubMessage msg)
        {
            requestIdCache.SetRequestId2Cache(requestId, future);
            Report(msg);

            if (future.Task.Wait(TimeSpan.FromMilliseconds(millisecondTimeout)))
            {
                return future.Task.Result;
            }
            else
            {
                return -1;
            }
        }

        public void ReportProperties(string deviceId, List<ServiceProperty> properties)
        {
            string topic = string.Format(TOPIC_REPORT_PROPERTY, bridgeId, deviceId);
            Report(new PubMessage(topic, properties));
        }

        public void ReportGatewaySubDeviceProperties(string deviceId, List<BridgeDeviceProperties> properties)
        {
            string topic = string.Format(TOPIC_REPORT_GATEWAY_PROPERTIES, bridgeId, deviceId);
            var msg = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "devices", properties }
            });
            Report(new PubMessage(topic, msg));
        }

        public void ResetSecret(string deviceId, string requestId, DeviceSecret deviceSecret)
        {
            string topic = string.Format(TOPIC_RESET_DEVICE_SECRET, bridgeId, deviceId, requestId);
            string payLoad = JsonUtil.ConvertObjectToJsonString(deviceSecret);
            Report(new PubMessage(topic, payLoad));
        }

        public void ReportDeviceMessage(string deviceId, DeviceMessage deviceMessage)
        {
            string topic = string.Format(TOPIC_REPORT_MESSAGE, bridgeId, deviceId);
            string payLoad = JsonUtil.ConvertObjectToJsonString(deviceMessage);
            Report(new PubMessage(topic, payLoad));
        }

        public void ReportRawDeviceMessage(string deviceId, RawDeviceMessage deviceMessage)
        {
            string topic = string.Format(TOPIC_REPORT_MESSAGE, bridgeId, deviceId);
            Report(new PubMessage(topic, deviceMessage.payload));
        }

        public void ReportEvent(string deviceId, DeviceEvents deviceEvent)
        {
            string topic = string.Format(TOPIC_REPORT_EVENT, bridgeId, deviceId);
            Report(new PubMessage(topic, deviceEvent));
        }

        public void RespondCommand(string deviceId, string requestId, CommandRsp commandRsp)
        {
            string topic = string.Format(TOPIC_COMMAND_RESPONSE, bridgeId, deviceId, requestId);
            string payload = JsonUtil.ConvertObjectToJsonString(commandRsp);
            Report(new PubMessage(topic, payload));
        }

        public void RespondPropsGet(string deviceId, string requestId, List<ServiceProperty> services)
        {
            string topic = string.Format(TOPIC_PROP_GET_RESPONSE, this.bridgeId, deviceId, requestId);
            DeviceProperties deviceProperties = new DeviceProperties();
            deviceProperties.services = services;
            Report(new PubMessage(topic, deviceProperties));
        }

        public void RespondPropsSet(string deviceId, string requestId, IotResult iotResult)
        {
            string topic = string.Format(TOPIC_PROP_SET_RESPONSE, bridgeId, deviceId, requestId);
            Report(new PubMessage(topic, JsonUtil.ConvertObjectToJsonString(iotResult)));
        }


        public string GetShadow(string deviceId, DeviceShadowRequest shadowRequest, string requestId = null)
        {
            requestId ??= Guid.NewGuid().ToString();
            var topic = string.Format(TOPIC_SHADOW_GET_REQ, bridgeId, deviceId, requestId);
            var msg = JsonConvert.SerializeObject(shadowRequest);
            Report(new PubMessage(topic, msg));
            return requestId;
        }

        public override void OnMessageReceived(RawMessage message)
        {
            string topic = message.Topic;

            if (!topic.Contains(BRIDGE_TOPIC_KEYWORD))
            {
                LOG.Error("The topic doesn't contain oc/bridges:{}", topic);
                return;
            }

            foreach (var listenItem in dict.Where(listenItem => topic.Contains(listenItem.Key)))
            {
                listenItem.Value.OnMessageReceived(this, message, topic.Contains("/request_id"));
                return;
            }

            LOG.Error("unknown topic:{}", topic);
        }
    }
}