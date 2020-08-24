using System;
using System.Collections.Generic;
using IoT.SDK.Device;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Transport;

namespace IoT.Device.Demo
{
    public class PropertiesGetAndSetSample : PropertyListener
    {
        private IoTDevice device;

        /// <summary>
        /// 通过Postman查询和设置平台属性
        /// </summary>
        public void FunPropertiesSample()
        {
            // 创建设备
            device = new IoTDevice("iot-mqtts.cn-north-4.myhuaweicloud.com", 8883, "5eb4cd4049a5ab087d7d4861_test_8746511", "12345678");

            if (device.Init() != 0)
            {
                return;
            }

            device.GetClient().propertyListener = this;
        }

        public void OnPropertiesSet(string requestId, string services)
        {
            Console.WriteLine("requestId Set:" + requestId);
            Console.WriteLine("services Set:" + services);

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
