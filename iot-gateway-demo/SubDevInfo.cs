using System.Collections.Generic;
using System.Text;
using IoT.SDK.Device.Gateway.Requests;
using IoT.SDK.Device.Utils;

namespace IoT.Gateway.Demo
{
    public class SubDevInfo
    {
        public long version { get; set; }

        public Dictionary<string, DeviceInfo> subdevices { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SubDevInfo{");
            sb.Append("version=" + version);
            sb.Append(", subdevices=" + JsonUtil.ConvertObjectToJsonString(subdevices));
            sb.Append("}");
            return sb.ToString();
        }
    }
}
