using System;
using IoT.SDK.Device;

namespace IoT.Device.Demo
{
    public class OTASample
    {
        /// <summary>
        /// OTA sample，用来演示如何实现设备升级。
        /// 使用方法：用户在平台上创建升级任务后，修改main函数里设备参数后启动本例，即可看到设备收到升级通知，并下   载升级包进行升级，
        /// 并上报升级结果。在平台上可以看到升级结果
        /// 前提条件：1、\download\test.bin 其中test.bin必须和服务器升级的软固件包名称一致
        /// 前提条件：2、\download\test.bin 其中根目录必须包含download文件夹（可根据情况自定义）
        /// </summary>
        public void FunOTASample()
        {
            // 创建设备
            IoTDevice device = new IoTDevice("iot-mqtts.cn-north-4.myhuaweicloud.com", 8883, "5eb4cd4049a5ab087d7d4861_test_8746511", "12345678");
            string packageSavePath = Environment.CurrentDirectory + @"\download\test.bin";
            OTAUpgrade otaSample = new OTAUpgrade(device, packageSavePath);
            otaSample.Init();
        }
    }
}
