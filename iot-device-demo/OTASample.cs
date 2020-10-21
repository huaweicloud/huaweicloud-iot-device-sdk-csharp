using IoT.SDK.Device;
using IoT.SDK.Device.Utils;

namespace IoT.Device.Demo
{
    public class OTASample
    {
        /// <summary>
        /// OTA sample，用来演示如何实现设备升级。
        /// 使用方法：用户在平台上创建升级任务后，修改main函数里设备参数后启动本例，即可看到设备收到升级通知，并下   载升级包进行升级，
        /// 并上报升级结果。在平台上可以看到升级结果
        /// 前提条件：\download\ 其中根目录必须包含download文件夹（可根据情况自定义）
        /// </summary>
        /// <param name="serverUri"></param>
        /// <param name="port"></param>
        /// <param name="deviceId"></param>
        /// <param name="deviceSecret"></param>
        public void FunOTASample(string serverUri, int port, string deviceId, string deviceSecret)
        {
            // 创建设备
            IoTDevice device = new IoTDevice(serverUri, port, deviceId, deviceSecret);

            // package路径必须包含软固件包名称及后缀
            string packageSavePath = IotUtil.GetRootDirectory() + @"\download\test.bin";
            OTAUpgrade otaSample = new OTAUpgrade(device, packageSavePath);
            otaSample.Init();
        }
    }
}
