using System;
using System.Collections.Generic;
using IoT.SDK.Device;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Transport;
using NLog;

namespace IoT.Device.Demo
{
    public class PropertySample : MessagePublishListener
    {
        public void FunPropertySample()
        {
            // 创建设备
            IoTDevice device = new IoTDevice("iot-mqtts.cn-north-4.myhuaweicloud.com", 1883, "5eb4cd4049a5ab087d7d4861_test_8746511", "12345678");

            if (device.Init() != 0)
            {
                return;
            }
            
            Dictionary<string, object> json = new Dictionary<string, object>();

            // 按照物模型设置属性
            json["alarm"] = 1;
            json["temperature"] = 23.45813;
            json["humidity"] = 56.89012;
            json["smokeConcentration"] = 89.56728;

            ServiceProperty serviceProperty = new ServiceProperty();
            serviceProperty.properties = json;
            serviceProperty.serviceId = "smokeDetector"; // serviceId要和物模型一致

            List<ServiceProperty> properties = new List<ServiceProperty>();
            properties.Add(serviceProperty);

            device.GetClient().messagePublishListener = this;
            device.GetClient().Report(new PubMessage(properties));
        }
        
        public void OnMessagePublished(RawMessage message)
        {
            Console.WriteLine("pubSuccessMessage:" + message.Payload);
            Console.WriteLine();
        }

        public void OnMessageUnPublished(RawMessage message)
        {
            Console.WriteLine("pubFailMessage:" + message.Payload);
            Console.WriteLine();
        }
    }
}
