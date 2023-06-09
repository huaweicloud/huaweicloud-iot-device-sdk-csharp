using IoT.SDK.Device;
using IoT.SDK.Device.Sdkinfo;

namespace IoT.Device.Demo
{
    public class DeviceInfoSample
    {
        public void FunDeviceInfoSample(string serverUri, int port, string deviceId, string deviceSecret)
        {
            // Creates a device.
            IoTDevice device = new IoTDevice(serverUri, port, deviceId, deviceSecret);

            if (device.Init() != 0)
            {
                return;
            }

            SdkInfoService sdkInfoService = device.sdkInfoService;
            
            sdkInfoService.ReportSdkInfoSync("v1.0", "v1.0");
        }
    }
}
