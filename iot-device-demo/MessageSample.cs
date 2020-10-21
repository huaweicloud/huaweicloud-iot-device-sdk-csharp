using System;
using IoT.SDK.Device;
using IoT.SDK.Device.Client.Listener;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Transport;
using NLog;

namespace IoT.Device.Demo
{
    public class MessageSample : MessagePublishListener, DeviceCustomMessageListener
    {
        public void FunMessageSample(string serverUri, int port, string deviceId, string deviceSecret)
        {
            // 创建设备
            IoTDevice device = new IoTDevice(serverUri, port, deviceId, deviceSecret);

            if (device.Init() != 0)
            {
                return;
            }

            device.GetClient().Report(new PubMessage(new DeviceMessage("hello11")));
            device.GetClient().deviceCustomMessageListener = this;
            device.GetClient().messagePublishListener = this;

            // 上报自定义topic消息，注意需要先在平台配置自定义topic,并且topic的前缀已经规定好，固定为：$oc/devices/{device_id}/user/，通过Postman模拟应用侧使用自定义Topic进行命令下发。
            string suf_topic = "wpy";
            device.GetClient().SubscribeTopic(suf_topic);

            device.GetClient().Report(new PubMessage(CommonTopic.PRE_TOPIC + suf_topic, "hello raw message "));
            ////while (true)
            ////{
            ////    Thread.Sleep(5000);
            ////}
        }

        public void OnMessagePublished(RawMessage message)
        {
            Console.WriteLine("pubSucessMessage:" + message.Payload);
            Console.WriteLine();
        }

        public void OnMessageUnPublished(RawMessage message)
        {
            Console.WriteLine("pubFailMessage:" + message.Payload);
            Console.WriteLine();
        }

        public void OnCustomMessageCommand(string message)
        {
            Console.WriteLine("onCustomMessageCommand , message = " + message);
        }
    }
}
