﻿using System;
using System.Collections.Generic;
using IoT.SDK.Device;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Utils;

namespace IoT.Device.Demo
{
    public class PropertiesGetAndSetSample : PropertyListener
    {
        private IoTDevice device;

        /// <summary>
        /// 通过Postman查询和设置平台属性
        /// </summary>
        /// <param name="serverUri"></param>
        /// <param name="port"></param>
        /// <param name="deviceId"></param>
        /// <param name="deviceSecret"></param>
        public void FunPropertiesSample(string serverUri, int port, string deviceId, string deviceSecret)
        {
            // 创建设备
            device = new IoTDevice(serverUri, port, deviceId, deviceSecret);

            if (device.Init() != 0)
            {
                return;
            }

            device.GetClient().propertyListener = this;
        }

        public void OnPropertiesSet(string requestId, List<ServiceProperty> services)
        {
            Console.WriteLine("requestId Set:" + requestId);
            Console.WriteLine("services Set:" + JsonUtil.ConvertObjectToJsonString(services));

            device.GetClient().Report(new PubMessage(CommonTopic.TOPIC_SYS_PROPERTIES_SET_RESPONSE + "=" + requestId, "{\"result_code\": 0,\"result_desc\": \"success\"}"));
        }

        public void OnPropertiesGet(string requestId, string serviceId)
        {
            Console.WriteLine("requestId Get:" + requestId);
            Console.WriteLine("serviceId Get:" + serviceId);

            Dictionary<string, object> json = new Dictionary<string, object>();

            // 按照物模型设置属性
            json["alarm"] = 1;
            json["temperature"] = 23.45813;
            json["humidity"] = 56.89012;
            json["smokeConcentration"] = 89.56728;

            ServiceProperty serviceProperty = new ServiceProperty();
            serviceProperty.properties = json;
            serviceProperty.serviceId = serviceId; // serviceId要和物模型一致

            List<ServiceProperty> properties = new List<ServiceProperty>();
            properties.Add(serviceProperty);

            device.GetClient().Report(new PubMessage(CommonTopic.TOPIC_SYS_PROPERTIES_GET_RESPONSE + "=" + requestId, properties));
        }
    }
}
