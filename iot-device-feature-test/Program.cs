using System.Threading;
using IoT.Device.Demo;
using IoT.Gateway.Demo;

namespace IoT.Device.Feature.Test
{
    class Program
    {
        private static ManualResetEvent mre = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            string serverUri = "iot-mqtts.cn-north-4.myhuaweicloud.com";
            string deviceId = "5eb4cd4049a5ab087d7d4861_demo";
            string deviceX509Id = "5eb4cd4049a5ab087d7d4861_x509_demo";
            string deviceSecret = "secret";

            string bootstrapUri = "iot-bs.cn-north-4.myhuaweicloud.com";
            string bDeviceId = "5eb4cd4049a5ab087d7d4861_lfd_test1343";
            string bDeviceX509Id = "5eb4cd4049a5ab087d7d4861_lfd_test_x509_1121";
            string bSecret = "7fa51ddf6f1b2c99ee20";

            ////MessageSample ms = new MessageSample();
            ////ms.FunMessageSample(serverUri, 1883, deviceId, deviceSecret);

            ////PropertySample ps = new PropertySample();
            ////ps.FunPropertySample(serverUri, 1883, deviceId, deviceSecret);

            ////CommandSample cs = new CommandSample();
            ////cs.FunCommandSample(serverUri, 8883, deviceId, deviceSecret);

            ////X509CertificateDeviceSample cd = new X509CertificateDeviceSample();
            ////cd.FunCertificateSample(serverUri, 8883, deviceX509Id);

            ////DeviceShadowSample ds = new DeviceShadowSample();
            ////ds.FunDeviceShadowSample(serverUri, 8883, deviceX509Id);

            ////PropertiesGetAndSetSample pgss = new PropertiesGetAndSetSample();
            ////pgss.FunPropertiesSample(serverUri, 8883, deviceId, deviceSecret);

            ////OTASample os = new OTASample();
            ////os.FunOTASample(serverUri, 8883, deviceId, deviceSecret);

            ////TimeSyncSample ts = new TimeSyncSample();
            ////ts.FunTimeSyncSample(serverUri, 1883, deviceId, deviceSecret);

            ////SmokeDetector sd = new SmokeDetector();
            ////sd.FunSmokeDetector(serverUri, 8883, deviceId, deviceSecret);

            ////new StringTcpServer(serverUri, 8883, deviceId, deviceSecret);

            ////BootstrapSample bs = new BootstrapSample();
            ////bs.FunBootstrapSample(bootstrapUri, 8883, bDeviceId, bSecret);

            ////BootsrapSelfRegSample bsr = new BootsrapSelfRegSample();
            ////bsr.FunBootsrapSelfRegSample(bootstrapUri, 8883, bDeviceX509Id);

            ////BootsrapGroupRegSample bgrs = new BootsrapGroupRegSample();
            ////bgrs.FunBootsrapGroupRegSample(bootstrapUri, 8883, "yourDeviceId", "yourScopeId");
            
            mre.WaitOne();
        }
    }
}
