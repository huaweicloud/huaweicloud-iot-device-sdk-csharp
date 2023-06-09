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
        public void FunPropertySample(string serverUri, int port, string deviceId, string deviceSecret)
        {
            // Creates a device.
            IoTDevice device = new IoTDevice(serverUri, port, deviceId, deviceSecret);

            if (device.Init() != 0)
            {
                return;
            }

            Dictionary<string, object> json = new Dictionary<string, object>();

            // Sets properties based on the product model.
            json["alarm"] = 1;
            json["temperature"] = 23.45812;
            json["humidity"] = 56.89013;
            json["smokeConcentration"] = 89.56724;

            ServiceProperty serviceProperty = new ServiceProperty();
            serviceProperty.properties = json;
            serviceProperty.serviceId = "smokeDetector"; // The serviceId must be the same as that defined in the product model.

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
