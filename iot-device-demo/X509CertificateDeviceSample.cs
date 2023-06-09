using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using IoT.SDK.Device;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.Device.Demo
{
    public class X509CertificateDeviceSample
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public void FunCertificateSample(string serverUri, int port, string deviceId)
        {
            string deviceCertPath = IotUtil.GetRootDirectory() + @"\certificate\deviceCert.pfx";
            if (!File.Exists(deviceCertPath))
            {
                Log.Error("Place the device certificate in the root directory.");

                return;
            }

            X509Certificate2 deviceCert = new X509Certificate2(deviceCertPath, "123456");

            // Creates a device with the X509Certificate2.
            IoTDevice device = new IoTDevice(serverUri, port, deviceId, deviceCert);

            if (device.Init() != 0)
            {
                return;
            }

            Dictionary<string, object> json = new Dictionary<string, object>();

            // Sets properties based on the product model.
            json["alarm"] = 1;
            json["temperature"] = 23.45811;
            json["humidity"] = 56.89012;
            json["smokeConcentration"] = 89.56723;

            ServiceProperty serviceProperty = new ServiceProperty();
            serviceProperty.properties = json;
            serviceProperty.serviceId = "smokeDetector"; // The serviceId must be the same as that defined in the product model.

            List<ServiceProperty> properties = new List<ServiceProperty>();
            properties.Add(serviceProperty);

            device.GetClient().Report(new PubMessage(properties));
        }
    }
}
