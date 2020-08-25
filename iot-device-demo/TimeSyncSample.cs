using System;
using IoT.SDK.Device;
using IoT.SDK.Device.Timesync;
using IoT.SDK.Device.Utils;

namespace IoT.Device.Demo
{
    public class TimeSyncSample : TimeSyncListener
    {
        public void FunTimeSyncSample()
        {
            // 创建设备
            IoTDevice device = new IoTDevice("iot-mqtts.cn-north-4.myhuaweicloud.com", 1883, "5eb4cd4049a5ab087d7d4861_test_8746511", "12345678");

            if (device.Init() != 0)
            {
                return;
            }

            TimeSyncService timeSyncService = device.timeSyncService;

            timeSyncService.listener = this;

            timeSyncService.RequestTimeSync();
        }

        public void OnTimeSyncResponse(long device_send_time, long server_recv_time, long server_send_time)
        {
            long device_recv_time = Convert.ToInt64(IotUtil.GetTimeStamp());
            long now = (server_recv_time + server_send_time + device_recv_time - device_send_time) / 2;
            Console.WriteLine("now is " + StampToDatetime(now));
        }

        public DateTime StampToDatetime(long timeStamp)
        {
            var startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)); // 当地时区

            // 返回转换后的日期
            return startTime.AddMilliseconds(timeStamp);
        }
    }
}
