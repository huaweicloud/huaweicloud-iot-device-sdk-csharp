using System;
using IoT.SDK.Device;
using IoT.SDK.Device.Timesync;
using IoT.SDK.Device.Utils;

namespace IoT.Device.Demo
{
    public class TimeSyncSample : TimeSyncListener
    {
        public void FunTimeSyncSample(string serverUri, int port, string deviceId, string deviceSecret)
        {
            // Creates a device.
            IoTDevice device = new IoTDevice(serverUri, port, deviceId, deviceSecret);

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
            var startTime = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Utc, TimeZoneInfo.Local); // Current time zone

            // Returns the date after the conversion.
            return startTime.AddMilliseconds(timeStamp);
        }
    }
}
