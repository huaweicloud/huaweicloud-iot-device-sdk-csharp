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
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using IoT.SDK.Bridge.Clent;
using IoT.SDK.Bridge.Listener;
using IoT.SDK.Bridge.Request;
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Client.Requests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace IoT.Device.Demo.HubDevice.Bridge
{
    class BridgeTest : LoginListener
        , BridgeCommandListener
        , BridgeDeviceMessageListener
        , LogoutListener
        , ResetDeviceSecretListener
        , BridgeDeviceDisConnListener
        , BridgePropertyListener
        , BridgeEventListener
        , BridgeShadowListener, BridgeRawDeviceMessageListener
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private BridgeClient client;
        private string rootDeviceId;
        private string rootDeviceSecret;
        private string subDeviceId;
        private string newDeviceSecret;
        private readonly RequestIdCache<object> requestIdCache = new RequestIdCache<object>();
        private const string PSEUDO_REQUEST_ID = "aiminabb";
        private const string PSEUDO_EVENT_REQUEST_ID = "event";
        private const string PSEUDO_MESSAGE_DOWN_REQUEST_ID = "message";
        private const string PSEUDO_COMMAND_DOWN_REQUEST_ID = "commad";
        private const string PSEUDO_PROPERTY_SET_REQUEST_ID = "property_set";
        private const string PSEUDO_PROPERTY_GET_REQUEST_ID = "property_get";
        private const string TEST_SERVICE_ID = "smokeDetector";

        private class DownLinkRequest
        {
            public string RequestId { get; set; }
            public object Payload { get; set; }
        }

        private void FinishByRequestId(string requestId, object result)
        {
            var future = requestIdCache.GetFuture(requestId);

            future?.SetResult(result);
        }

        private void StartOneTest(string name, Func<string> doAction, Func<object, bool> checkResult)
        {
            LOG.Info(" start >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> {}", name);
            string requestId = doAction.Invoke() ?? PSEUDO_REQUEST_ID;
            StartOneTestCommon(name, requestId, checkResult, TimeSpan.FromSeconds(5));
            LOG.Info("end <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< {}", name);
        }
        private void StartOneTest(string name, Action preSet, Func<string> doAction, Func<object, bool> checkResult, int retryTime)
        {
            LOG.Info(" start >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> {}", name);
            preSet.Invoke();

            for (int i = 0; i < retryTime; i++)
            {
                try
                {
                    string requestId = doAction.Invoke() ?? PSEUDO_REQUEST_ID;
                    StartOneTestCommon(name, requestId, checkResult, TimeSpan.FromSeconds(5));
                    break;
                }
                catch (VerificationException)
                {
                    if (i + 1 == retryTime)
                    {
                        throw;
                    }

                }
                Thread.Sleep(TimeSpan.FromMilliseconds(500));

            }
            LOG.Info("end <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< {}", name);

        }
        private void StartOneManualTest(string name, string requestId, Func<object, bool> checkResult)
        {
            LOG.Info(" start >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> {}", name);

            StartOneTestCommon(name, requestId, checkResult, TimeSpan.FromMinutes(20));
            LOG.Info("end <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< {}", name);
        }

        private void StartOneTestCommon(string name, string requestId, Func<object, bool> checkResult,
            TimeSpan waitTime)
        {
            var future = new TaskCompletionSource<object>();
            requestIdCache.SetRequestId2Cache(requestId, future);
            if (!future.Task.Wait(waitTime))
            {
                throw new Exception(name + " failed, timeout");
            }

            if (!checkResult.Invoke(future.Task.Result))
            {

                LOG.Warn("verify failed: {}", future.Task.Result);
                throw new VerificationException(name + " result check failed");
            }

        }

        public void OnLogin(string deviceId, string requestId, int resultCode)
        {
            LOG.Info("OnLogin deviceId:{}, requestId:{}, resultCode:{}", deviceId, requestId,
                resultCode);
            FinishByRequestId(requestId, new Dictionary<string, object>
            {
                { "deviceId", deviceId },
                { "resultCode", resultCode },
            });
        }


        public void OnCommand(string deviceId, string requestId, BridgeCommand bridgeCommand)
        {
            LOG.Info("OnCommand deviceId:{}, requestId:{}, bridgeCommand:{}", deviceId, requestId,
                JsonConvert.SerializeObject(bridgeCommand));

            FinishByRequestId(PSEUDO_COMMAND_DOWN_REQUEST_ID, new DownLinkRequest
            {
                RequestId = requestId,
                Payload = bridgeCommand
            });
        }

        public void OnDeviceMessage(string deviceId, DeviceMessage deviceMessage)
        {
            LOG.Info("OnDeviceMessage device id:{},  deviceMessage:{}", deviceId,
                JsonConvert.SerializeObject(deviceMessage));
            FinishByRequestId(PSEUDO_MESSAGE_DOWN_REQUEST_ID, deviceMessage);
        }

        public void OnRawDeviceMessage(string deviceId, RawDeviceMessage deviceMessage)
        {
            FinishByRequestId(PSEUDO_MESSAGE_DOWN_REQUEST_ID, deviceMessage);
        }

        public void OnLogout(string deviceId, string requestId, Dictionary<string, object> map)
        {
            FinishByRequestId(requestId, new Dictionary<string, object>
            {
                { "deviceId", deviceId },
                { "resultCode", Convert.ToInt32(map["result_code"]) },
            });
        }


        public void OnResetDeviceSecret(string deviceId, string requestId, int resultCode, string newSecret)
        {
            LOG.Info("OnResetDeviceSecret deviceId:{}, requestId:{}, resultCode:{}, newSecret:{}",
                deviceId, requestId,
                resultCode, newSecret);
            FinishByRequestId(requestId, new Dictionary<string, object>
            {
                { "deviceId", deviceId },
                { "resultCode", resultCode },
                { "newSecret", newSecret },
            });
        }

        public void OnDisConnect(string deviceId)
        {
            FinishByRequestId(PSEUDO_REQUEST_ID, deviceId);
        }


        public void OnPropertiesSet(string deviceId, string requestId, List<ServiceProperty> services)
        {
            FinishByRequestId(PSEUDO_PROPERTY_SET_REQUEST_ID, new DownLinkRequest
            {
                RequestId = requestId,
                Payload = services
            });
        }

        public void OnPropertiesGet(string deviceId, string requestId, string serviceId)
        {
            FinishByRequestId(PSEUDO_PROPERTY_GET_REQUEST_ID, new DownLinkRequest
            {
                RequestId = requestId,
                Payload = serviceId
            });
        }


        public void OnEvent(string deviceId, DeviceEvents deviceEvents)
        {
            FinishByRequestId(PSEUDO_EVENT_REQUEST_ID, deviceEvents);
        }


        public void OnShadowDown(string deviceId, string requestId, DeviceShadowResponse shadows)
        {
            FinishByRequestId(requestId, shadows);
        }


        private bool TestAsyncLoginLogoutResultCheck(object reply)
        {
            var receivedPayload = (Dictionary<string, object>)reply;
            return !((string)receivedPayload["deviceId"] != rootDeviceId ||
                     (int)receivedPayload["resultCode"] != 0);
        }

        private void TestAsyncLoginLogout()
        {
            client.loginListener = this;
            client.logoutListener = this;
            StartOneTest("async login",
                () => client.LoginAsync(rootDeviceId, rootDeviceSecret),
                TestAsyncLoginLogoutResultCheck);

            StartOneTest("async logout",
                () => client.LoginAsync(rootDeviceId, rootDeviceSecret),
                TestAsyncLoginLogoutResultCheck);

            client.loginListener = null;
            client.logoutListener = null;
        }

        private void TestSyncLoginLogout()
        {
            if (client.LoginSync(rootDeviceId, rootDeviceSecret, 5000) != 0)
            {
                throw new Exception("login failed");
            }


            if (client.LogoutSync(rootDeviceId, 5000) != 0)
            {
                throw new Exception("logout failed");
            }
        }

        private static ServiceProperty GenerateTestServices()
        {
            var r = new Random();

            var properties = new Dictionary<string, object>
            {
                { "alarm", r.Next() % 2 },
                { "smokeConcentration", r.NextDouble() % 100 },
                { "temperature", r.NextDouble() % 100 },
                { "humidity", r.Next() % 100 },
            };
            return new ServiceProperty
            {
                serviceId = TEST_SERVICE_ID,
                properties = properties
            };
        }

        private void TestReportProperties()
        {
            var service = GenerateTestServices();
            StartOneTest("report properties and verify through getting shadow",
                () =>
                {
                    client.ReportProperties(rootDeviceId, new List<ServiceProperty>
                    {
                        service
                    });
                },
                () =>
                {
                    return client.GetShadow(rootDeviceId, new DeviceShadowRequest
                    { ServiceId = service.serviceId, DeviceId = rootDeviceId });
                    // wait for shadow to refresh
                },
                (reply) =>
                {
                    var resp = (DeviceShadowResponse)reply;
                    Debug.Assert(resp.Shadow.Count == 1);
                    Debug.Assert(resp.Shadow[0].ServiceId == service.serviceId);
                    var reported = JObject.FromObject(resp.Shadow[0].Reported.Properties);
                    var propertiesJ = JObject.FromObject(service.properties);


                    return JToken.DeepEquals(reported, propertiesJ);
                }, 10);
        }

        private void TestReportGatewaySubDeviceProperties()
        {
            var service = GenerateTestServices();
            var deviceProperties = new BridgeDeviceProperties
            {
                DeviceId = subDeviceId,
                Services = new List<ServiceProperty>
                {
                    service
                }
            };
            StartOneTest("report gateway sub device properties and verify through getting shadow",
                () =>
                {
                    client.ReportGatewaySubDeviceProperties(rootDeviceId, new List<BridgeDeviceProperties>
                    {
                        deviceProperties
                    });
                },
                () =>
                {
                    return client.GetShadow(rootDeviceId,
                        new DeviceShadowRequest { ServiceId = service.serviceId, DeviceId = subDeviceId });
                },
                reply =>
                {
                    var resp = (DeviceShadowResponse)reply;
                    Debug.Assert(resp.Shadow.Count == 1);
                    Debug.Assert(resp.Shadow[0].ServiceId == service.serviceId);
                    var reported = JObject.FromObject(resp.Shadow[0].Reported.Properties);
                    var propertiesJ = JObject.FromObject(service.properties);


                    return JToken.DeepEquals(reported, propertiesJ);
                }, 10);
        }

        private void TestResetSecret()
        {
            StartOneTest("reset password",
                () =>
                {
                    var requestId = Guid.NewGuid().ToString();
                    client.ResetSecret(rootDeviceId, requestId,
                        new DeviceSecret(rootDeviceSecret, newDeviceSecret));
                    return requestId;
                },
                reply =>
                {
                    var expectedReply = new JObject
                    {
                        { "deviceId", rootDeviceId },
                        { "resultCode", 0 },
                        { "newSecret", newDeviceSecret },
                    };

                    return JToken.DeepEquals(JObject.FromObject(reply), expectedReply);
                });
            StartOneTest("reset password again",
                () =>
                {
                    var requestId = Guid.NewGuid().ToString();
                    client.ResetSecret(rootDeviceId, requestId,
                        new DeviceSecret(newDeviceSecret, rootDeviceSecret));
                    return requestId;
                },
                reply =>
                {
                    var expectedReply = new JObject
                    {
                        { "deviceId", rootDeviceId },
                        { "resultCode", 0 },
                        { "newSecret", rootDeviceSecret },
                    };

                    return JToken.DeepEquals(JObject.FromObject(reply), expectedReply);
                });
        }

        private void TestReportEvent()
        {
            StartOneTest("report event",
                () =>
                {
                    client.ReportEvent(rootDeviceId, new DeviceEvents
                    {
                        services = new List<DeviceEvent>{
                            new DeviceEvent {
                                serviceId = "$sub_device_manager",
                                eventType = "sub_device_sync_request",
                                paras = new Dictionary<string, object>
                                {
                                    { "version", 0 }
                                }
                            }
                        }
                    });
                    return PSEUDO_EVENT_REQUEST_ID;
                },
                reply =>
                {
                    var r = (DeviceEvents)reply;
                    Debug.Assert(r.services.Count == 1);
                    var service = r.services[0];
                    Debug.Assert(service.serviceId == "$sub_device_manager");
                    JArray devices = (JArray)service.paras["devices"];
                    var found = devices.Any(v => ((JObject)v)["device_id"].ToString() == subDeviceId);

                    return found;
                });
        }

        private void TesMessageDownThenReport()
        {
            client.bridgeDeviceMessageListener = this;
            StartOneManualTest("test message down then report",
                PSEUDO_MESSAGE_DOWN_REQUEST_ID,
                (reply) =>
                {
                    var r = (DeviceMessage)reply;
                    r.id = Guid.NewGuid().ToString();
                    client.ReportDeviceMessage(rootDeviceId, r);
                    return true;
                });
            client.bridgeDeviceMessageListener = null;

            client.BridgeRawDeviceMessageListener = this;
            StartOneManualTest("test raw message down then report",
                PSEUDO_MESSAGE_DOWN_REQUEST_ID,
                (reply) =>
                {
                    var r = (RawDeviceMessage)reply;
                    client.ReportRawDeviceMessage(rootDeviceId, r);
                    return true;
                });
        }

        private void TestCommandDown()
        {
            StartOneManualTest("test command down",
                PSEUDO_COMMAND_DOWN_REQUEST_ID,
                (reply) =>
                {
                    var r = (DownLinkRequest)reply;
                    var requestId = r.RequestId;
                    var bridgeCommand = (BridgeCommand)r.Payload;
                    Debug.Assert(bridgeCommand.command.serviceId == TEST_SERVICE_ID);
                    Debug.Assert(bridgeCommand.command.paras.ContainsKey("duration"));
                    var rsp = new CommandRsp(CommandRsp.SUCCESS,
                        new Dictionary<string, object>
                        {
                            { "set", true }
                        })
                    {
                        responseName = "ringAlarm",
                    };
                    client.RespondCommand(bridgeCommand.deviceId, requestId, rsp);
                    return true;
                });
        }


        private void TestPropertyGet()
        {
            StartOneManualTest("test property get",
                PSEUDO_PROPERTY_GET_REQUEST_ID,
                (reply) =>
                {
                    var r = (DownLinkRequest)reply;
                    var requestId = r.RequestId;
                    var serviceId = (string)r.Payload;

                    var service = GenerateTestServices();
                    client.RespondPropsGet(rootDeviceId, requestId, new List<ServiceProperty>
                    {
                        service
                    });
                    return serviceId == TEST_SERVICE_ID;
                });
        }

        private void TestPropertySet()
        {
            StartOneManualTest("test property set",
                PSEUDO_PROPERTY_SET_REQUEST_ID,
                (reply) =>
                {
                    var r = (DownLinkRequest)reply;
                    var requestId = r.RequestId;
                    var services = (List<ServiceProperty>)r.Payload;
                    var result = services.Count == 1 && services[0].serviceId == TEST_SERVICE_ID
                                                     && services[0].properties.ContainsKey("alarm")
                                                     && services[0].properties.ContainsKey("smokeConcentration")
                                                     && services[0].properties.ContainsKey("temperature")
                                                     && services[0].properties.ContainsKey("humidity");
                    client.RespondPropsSet(rootDeviceId, requestId, new IotResult(result ? 0 : 1, "aitest"));
                    return result;
                });
        }


        public void Start(BridgeClient bridgeClient)
        {
            // 启动时初始化网桥连接
            client = bridgeClient;
            rootDeviceId = Environment.GetEnvironmentVariable("ENV_ROOT_DEVICE_ID");
            rootDeviceSecret = Environment.GetEnvironmentVariable("ENV_ROOT_DEVICE_SECRET");
            subDeviceId = Environment.GetEnvironmentVariable("ENV_SUB_DEVICE_ID");
            newDeviceSecret = Environment.GetEnvironmentVariable("ENV_ROOT_DEV_NEW_PW");
            client.bridgeCommandListener = this;
            client.resetDeviceSecretListener = this;
            client.bridgeDeviceDisConnListener = this;
            client.bridgePropertyListener = this;
            client.BridgeEventListener = this;
            client.BridgeShadowListener = this;

            TestAsyncLoginLogout();
            TestSyncLoginLogout();

            client.LoginSync(rootDeviceId, rootDeviceSecret, 5000);

            TestReportProperties();
            TestReportGatewaySubDeviceProperties();
            TestResetSecret();
            TestReportEvent();
            var parallelManualTests = new List<Task>
            {
                Task.Run(TesMessageDownThenReport),
                Task.Run(TestCommandDown),
                Task.Run(TestPropertyGet),
                Task.Run(TestPropertySet),
            };

            Task.WaitAll(parallelManualTests.ToArray());


            client.LogoutSync(rootDeviceId, 5000);
        }
    }
}