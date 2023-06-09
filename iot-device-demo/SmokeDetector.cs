using IoT.SDK.Device;

namespace IoT.Device.Demo
{
    public class SmokeDetector
    {
        public void FunSmokeDetector(string serverUri, int port, string deviceId, string deviceSecret)
        {
            // Creates a device.
            IoTDevice device = new IoTDevice(serverUri, port, deviceId, deviceSecret);

            if (device.Init() != 0)
            {
                return;
            }

            // Creates a service.
            SmokeDetectorService smokeDetectorService = new SmokeDetectorService();
            device.AddService("smokeDetector", smokeDetectorService);

            // Enables automatic, periodic reporting.
            smokeDetectorService.EnableAutoReport(10000);

            smokeDetectorService.FirePropertiesChanged();
        }
    }
}
