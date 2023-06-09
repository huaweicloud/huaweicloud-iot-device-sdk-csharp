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
            // Creates a device.
            IoTDevice device = new IoTDevice(serverUri, port, deviceId, deviceSecret);

            if (device.Init() != 0)
            {
                return;
            }

            device.GetClient().Report(new PubMessage(new DeviceMessage("hello11")));
            device.GetClient().deviceCustomMessageListener = this;
            device.GetClient().messagePublishListener = this;

            // Reports a message with a custom topic. You must configure the custom topic on the platform and set the topic prefix to $oc/devices/{device_id}/user/. Use Postman to simulate the scenario in which an application uses the custom topic to deliver a command.
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
