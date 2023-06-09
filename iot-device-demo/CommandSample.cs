using System;
using System.Collections.Generic;
using IoT.SDK.Device;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Utils;

namespace IoT.Device.Demo
{
    /// <summary>
    /// Demonstrates how to use DeviceClient to process a command delivered by the platform.
    /// </summary>
    public class CommandSample : CommandListener
    {
        private IoTDevice device;

        public void FunCommandSample(string serverUri, int port, string deviceId, string deviceSecret)
        {
            // Creates a device.
            device = new IoTDevice(serverUri, port, deviceId, deviceSecret);

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

            ////Processes a command.

            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("result", "success");

            // Sends a command response.
            device.GetClient().Report(new PubMessage(requestId, new CommandRsp(0, dic)));
        }
    }
}
