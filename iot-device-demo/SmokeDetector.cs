using IoT.SDK.Device;

namespace IoT.Device.Demo
{
    public class SmokeDetector
    {
        public void FunSmokeDetector(string serverUri, int port, string deviceId, string deviceSecret)
        {
            // 创建设备
            IoTDevice device = new IoTDevice(serverUri, port, deviceId, deviceSecret);

            if (device.Init() != 0)
            {
                return;
            }

            // 创建设备服务
            SmokeDetectorService smokeDetectorService = new SmokeDetectorService();
            device.AddService("smokeDetector", smokeDetectorService);

            // 启动自动周期上报
            smokeDetectorService.EnableAutoReport(10000);

            smokeDetectorService.FirePropertiesChanged();
        }
    }
}
