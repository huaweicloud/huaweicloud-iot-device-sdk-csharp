using IoT.SDK.Device;
using IoT.SDK.Device.Utils;

namespace IoT.Device.Demo
{
    public class OTASample
    {
        /// <summary>
        /// Demonstrates how to upgrade devices.
        /// Usage: After creating an upgrade task on the platform, modify the device parameters in the main function and start this sample.
        /// The device receives the upgrade notification, downloads the upgrade package, and reports the upgrade result.
        /// The upgrade result is displayed on the platform.
        /// Prerequisites: \download\ The root directory must contain the download folder (which can be customized as required).
        /// </summary>
        /// <param name="serverUri"></param>
        /// <param name="port"></param>
        /// <param name="deviceId"></param>
        /// <param name="deviceSecret"></param>
        public void FunOTASample(string serverUri, int port, string deviceId, string deviceSecret)
        {
            // Creates a device.
            IoTDevice device = new IoTDevice(serverUri, port, deviceId, deviceSecret);

            // The package path must contain the software or firmware package name and extension.
            string packageSavePath = IotUtil.GetRootDirectory() + @"\download\test.bin";
            OTAUpgrade otaSample = new OTAUpgrade(device, packageSavePath);
            otaSample.Init();
        }
    }
}
