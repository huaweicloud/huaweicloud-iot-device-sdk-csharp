using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using IoT.SDK.Device;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Transport;
using NLog;

namespace IoT.Device.Demo
{
    public class X509CertificateDeviceSample
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public void FunCertificateSample()
        {
            string deviceCertPath = Environment.CurrentDirectory + @"\certificate\deviceCert.pfx";
            if (!File.Exists(deviceCertPath))
            {
                Log.Error("请将设备证书放到根目录！");

                return;
            }

            X509Certificate2 deviceCert = new X509Certificate2(deviceCertPath, "123456");

            // 使用证书创建设备
            IoTDevice device = new IoTDevice("iot-mqtts.cn-north-4.myhuaweicloud.com", 8883, "5eb4cd4049a5ab087d7d4861_test_x509_789456", deviceCert);
            
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
            
            device.GetClient().Report(new PubMessage(properties));
        }
    }
}
