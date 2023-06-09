using System;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Service;
using Newtonsoft.Json.Linq;

namespace IoT.Device.Demo
{
    /// <summary>
    /// Smoke sensor service. Supported properties: alarm flag, smoke density, temperature, and humidity.
    /// Supported command: ring alarm
    /// </summary>
    public class SmokeDetectorService : AbstractService
    {
        public SmokeDetectorService()
        {
            this.SetDeviceService(this);
        }

        // Defines propreties based on the product model. Note that the property name and type must be the same as those defined in the product model.
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
                // Simulate data reading from a sensor.
                return (float)new Random().NextDouble();
            }
        }

        /// <summary>
        /// Defines a command. Note that the input parameters and return value types of the method are fixed and cannot be modified. Otherwise, a runtime error occurs.
        /// The method name must be the same as the command name defined in the product model.
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
