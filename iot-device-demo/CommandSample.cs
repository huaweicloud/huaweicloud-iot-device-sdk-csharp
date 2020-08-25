using System;
using System.Collections.Generic;
using IoT.SDK.Device;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.Device.Demo
{
    /// <summary>
    /// 演示如何直接使用DeviceClient处理平台下发的命令
    /// </summary>
    public class CommandSample : CommandListener
    {
        private IoTDevice device;

        public void FunCommandSample()
        {
            // 创建设备
            device = new IoTDevice("iot-mqtts.cn-north-4.myhuaweicloud.com", 8883, "5eb4cd4049a5ab087d7d4861_test_8746511", "12345678");

            if (device.Init() != 0)
            {
                return;
            }

            device.GetClient().commandListener = this;
        }

        public void OnCommand(string requestId, string serviceId, string commandName, Dictionary<string, object> paras)
        {
            Console.WriteLine("onCommand, serviceId = " + serviceId);
            Console.WriteLine("onCommand, name = " + commandName);
            Console.WriteLine("onCommand, paras =  " + JsonUtil.ConvertObjectToJsonString(paras));

            ////处理命令
            
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("result", "success");

            // 发送命令响应
            device.GetClient().Report(new PubMessage(requestId, new CommandRsp(0, dic)));
        }
    }
}
