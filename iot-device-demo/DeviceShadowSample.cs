using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using IoT.SDK.Device;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.Device.Demo
{
    /// <summary>
    /// Obtains device shadow data.
    /// </summary>
    public class DeviceShadowSample : DeviceShadowListener
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private IoTDevice device;

        public void FunDeviceShadowSample(string serverUri, int port, string deviceId)
        {
            // Creates a device.
            string deviceCertPath = IotUtil.GetRootDirectory() + @"\certificate\deviceCert.pfx";
            if (!File.Exists(deviceCertPath))
            {
                Log.Error("Place the device certificate in the root directory.");

                return;
            }

            X509Certificate2 deviceCert = new X509Certificate2(deviceCertPath, "123456");

            // Creates a device with X509Certificate2.
            device = new IoTDevice(serverUri, port, deviceId, deviceCert);

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
