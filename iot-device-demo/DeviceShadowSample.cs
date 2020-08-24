using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using IoT.SDK.Device;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Transport;
using NLog;

namespace IoT.Device.Demo
{
    /// <summary>
    /// 获取影子数据
    /// </summary>
    public class DeviceShadowSample : DeviceShadowListener
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private IoTDevice device;

        public void FunDeviceShadowSample()
        {
            // 创建设备
            string deviceCertPath = Environment.CurrentDirectory + @"\certificate\deviceCert.pfx";
            if (!File.Exists(deviceCertPath))
            {
                Log.Error("请将设备证书放到根目录！");

                return;
            }

            X509Certificate2 deviceCert = new X509Certificate2(deviceCertPath, "123456");

            // 使用证书创建设备
            device = new IoTDevice("iot-mqtts.cn-north-4.myhuaweicloud.com", 8883, "5eb4cd4049a5ab087d7d4861_test_x509_789456", deviceCert);

            if (device.Init() != 0)
            {
                return;
            }

            device.GetClient().deviceShadowListener = this;

            string guid = Guid.NewGuid().ToString();

            Console.WriteLine(guid);

            string topic = CommonTopic.TOPIC_SYS_SHADOW_GET + "=" + guid;

            device.GetClient().Report(new PubMessage(topic, string.Empty));
        }

        public void OnShadowCommand(string requestId, string message)
        {
            Console.WriteLine(requestId);
            Console.WriteLine(message);
        }
    }
}
