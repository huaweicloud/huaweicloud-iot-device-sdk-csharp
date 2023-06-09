using System.IO;
using System.Security.Cryptography.X509Certificates;
using IoT.SDK.Device;
using IoT.SDK.Device.Bootstrap;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace IoT.Device.Demo
{
    public class BootsrapGroupRegSample : BootstrapMessageListener
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private string bootstrapUri;

        private int port;

        private string scopeId;

        BootstrapClient bootstrapClient;

        private string deviceId;

        private X509Certificate2 deviceCert;

        private IoTDevice device;

        public void FunBootsrapGroupRegSample(string bootstrapUri, int port, string deviceId, string scopeId)
        {
            this.bootstrapUri = bootstrapUri;
            this.port = port;
            this.deviceId = deviceId;
            this.scopeId = scopeId;

            string deviceCertPath = IotUtil.GetRootDirectory() + @"\certificate\deviceCert.pfx";
            if (!File.Exists(deviceCertPath))
            {
                Log.Error("Place the device certificate in the root directory.");

                return;
            }

            deviceCert = new X509Certificate2(deviceCertPath, "123456");
            
            bootstrapClient = new BootstrapClient(bootstrapUri, port, deviceId, deviceCert, scopeId); // 设备组方式用到

            bootstrapClient.bootstrapMessageListener = this;

            bootstrapClient.Bootstrap();
        }

        public void OnBootstrapMessage(string payload)
        {
            JObject obj = JObject.Parse(payload);

            string address = obj["address"].ToString();

            Log.Info("bootstrap success:" + address);

            // 引导成功后关闭客户端
            bootstrapClient.Close();

            string serverUri = address.Split(':')[0];

            int port = int.Parse(address.Split(':')[1]);

            device = new IoTDevice(serverUri, port, deviceId, deviceCert);

            if (device.Init() != 0)
            {
                return;
            }

            device.GetClient().ReportDeviceMessage(new DeviceMessage("hello"));

            device.GetClient().bootstrapMessageListener = this;
        }

        public void OnRetryBootstrapMessage()
        {
            //断开已有设备并重引导
            device.GetClient().Close();

            bootstrapClient = new BootstrapClient(bootstrapUri, port, deviceId, deviceCert, scopeId);

            bootstrapClient.bootstrapMessageListener = this;

            bootstrapClient.Bootstrap();
        }
    }
}
