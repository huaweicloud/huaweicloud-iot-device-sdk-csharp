using System;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Service;
using Newtonsoft.Json.Linq;

namespace IoT.Device.Demo
{
    /// <summary>
    /// 烟感服务，支持属性：报警标志、烟雾浓度、温度、湿度
    /// 支持的命令：响铃报警
    /// </summary>
    public class SmokeDetectorService : AbstractService
    {
        public SmokeDetectorService()
        {
            this.SetDeviceService(this);
        }

        // 按照设备模型定义属性，注意属性的name和类型需要和模型一致
        [Property(Name = "alarm", Writeable = true)]
        public int smokeAlarm { get; set; } = 1;

        [Property(Name = "smokeConcentration", Writeable = false)]
        public float concentration
        {
            get
            {
                return (float)new Random().NextDouble();
            }
        }

        [Property(Writeable = false)]
        public int humidity { get; set; }
        
        [Property(Writeable = false)]
        public float temperature
        {
            get
            {
                // 模拟从传感器读取数据
                return (float)new Random().NextDouble();
            }
        }

        /// <summary>
        /// 定义命令，注意接口入参和返回值类型是固定的不能修改，否则会出现运行时错误
        /// 方法名和模型命令一致
        /// </summary>
        /// <param name="jsonParas"></param>
        /// <returns></returns>
        [DeviceCommand(Name = "ringAlarm")]
        public CommandRsp alarm(string jsonParas)
        {
            JObject obj = JObject.Parse(jsonParas);
            int value = (int)obj["value"];
            
            return new CommandRsp(0);
        }
    }
}
