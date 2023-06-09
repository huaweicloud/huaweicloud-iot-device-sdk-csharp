using IoT.SDK.Device;
using IoT.SDK.Device.Bootstrap;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace IoT.Device.Demo
{
    public class BootsrapSelfRegSample : BootstrapMessageListener
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        BootstrapClient bootstrapClient;

        private string bootstrapUri;

        private int port;

        private string deviceId;

        private IoTDevice device;
        
        private X509Certificate2 deviceCert;

        public void FunBootsrapSelfRegSample(string bootstrapUri, int port, string deviceId)
        {
            this.bootstrapUri = bootstrapUri;

            this.deviceId = deviceId;

            this.port = port;

            string deviceCertPath = IotUtil.GetRootDirectory() + @"\certificate\deviceCert.pfx";
            if (!File.Exists(deviceCertPath))
            {
                Log.Error("Place the device certificate in the root directory.");

                return;
            }

            deviceCert = new X509Certificate2(deviceCertPath, "123456");

            //创建引导客户端，发起引导
            bootstrapClient = new BootstrapClient(bootstrapUri, port, deviceId, deviceCert);
            
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

            bootstrapClient = new BootstrapClient(bootstrapUri, port, deviceId, deviceCert);

            bootstrapClient.bootstrapMessageListener = this;

            bootstrapClient.Bootstrap();
        }
    }
}
