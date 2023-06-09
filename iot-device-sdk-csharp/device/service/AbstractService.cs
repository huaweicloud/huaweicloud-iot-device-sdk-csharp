/*
 * Copyright (c) 2020-2020 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this list of
 *    conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list
 *    of conditions and the following disclaimer in the documentation and/or other materials
 *    provided with the distribution.
 *
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used
 *    to endorse or promote products derived from this software without specific prior written
 *    permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.SDK.Device.Service
{
    /// <summary>
    /// Provides automatic properties read/write and command invoking capabilities. You can inherit this class and define your own services based on the product model.
    /// </summary>
    public class AbstractService : IService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private Timer timer;

        private object deviceService;

        private Type deviceServiceType;

        private Dictionary<string, MethodInfo> commands = new Dictionary<string, MethodInfo>();

        private Dictionary<string, PropertyInfo> writeableFields = new Dictionary<string, PropertyInfo>();

        private Dictionary<string, PropertyInfo> readableFields = new Dictionary<string, PropertyInfo>();

        public AbstractDevice iotDevice { get; set; }

        public string ServiceId { get; set; }

        public CommandRsp OnCommand(Command command)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();

            if (!commands.ContainsKey(command.commandName))
            {
                dic.Clear();
                dic.Add("result", "command not found in commands");
                Log.Error("command not found in commands" + command.commandName);
                return new CommandRsp(CommandRsp.FAIL, dic);
            }

            MethodInfo methodInfo = commands[command.commandName];
            if (methodInfo == null)
            {
                dic.Clear();
                dic.Add("result", "command not found");
                Log.Error("command not found " + command.commandName);
                return new CommandRsp(CommandRsp.FAIL, dic);
            }

            try
            {
                dic.Clear();
                dic.Add("result", "success");

                // A reflection call method.
                MethodInfo nonstaticMethod = deviceServiceType.GetMethod(methodInfo.Name);

                // Class instance required for non-static method calls.
                object obj = nonstaticMethod.Invoke(deviceService, new string[] { JsonUtil.ConvertObjectToJsonString(command.paras) });
                
                return (CommandRsp)obj;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                dic.Clear();
                dic.Add("result", e.Message);
                return new CommandRsp(CommandRsp.FAIL, dic);
            }
        }

        /// <summary>
        /// Called when an event delivered by the platform is received. The default implementation does nothing.
        /// </summary>
        /// <param name="deviceEvent"></param>
        public virtual void OnEvent(DeviceEvent deviceEvent)
        {
            Log.Info("onEvent no op");
        }

        public void SetDeviceService<T>(T deviceService)
        {
            this.deviceService = deviceService;
            deviceServiceType = typeof(T); // Reflection object
            var deviceServiceProperties = deviceServiceType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance); // Obtains the device properties.

            foreach (PropertyInfo servicePro in deviceServiceProperties)
            {
                // Console.WriteLine(b.Name + ":" + b.GetValue(model));
                string name = servicePro.Name;

                foreach (object attributes in servicePro.GetCustomAttributes(false))
                {
                    Property property = (Property)attributes;
                    if (property != null)
                    {
                        if (!string.IsNullOrEmpty(property.Name))
                        {
                            name = property.Name;
                        }

                        if (property.Writeable)
                        {
                            writeableFields.Add(name, servicePro);
                        }
                    }
                }

                readableFields.Add(name, servicePro);
            }

            MethodInfo[] deviceServiceMethods = deviceServiceType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            foreach (MethodInfo deviceServiceMethod in deviceServiceMethods)
            {
                string name = deviceServiceMethod.Name;
                
                foreach (Attribute a in deviceServiceMethod.GetCustomAttributes(true))
                {
                    if (name.Contains("get_") || name.Contains("set_"))
                    {
                        continue;
                    }

                    DeviceCommand deviceCommand = (DeviceCommand)a;
                    if (deviceCommand != null)
                    {
                        name = deviceCommand.Name;
                    }
                }

                // Console.WriteLine("Field:{0}", f.Name);
                commands.Add(name, deviceServiceMethod);
            }
        }

        /// <summary>
        /// Called when a property query request is received.
        /// </summary>
        /// <param name="properties">Indicates the names of fields to read. If it is set to NULL, all readable fields are read.</param>
        /// <returns>Returns the property values.</returns>
        public Dictionary<string, object> OnRead(params string[] properties)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();

            // Reads specified fields.
            if (properties.Length > 0)
            {
                foreach (string propertyName in properties)
                {
                    if (readableFields[propertyName] == null)
                    {
                        Log.Error("field is not readable:" + propertyName);
                        continue;
                    }

                    object value = GetFiledValue(propertyName);
                    if (value != null)
                    {
                        ret.Add(propertyName, value);
                    }
                }

                return ret;
            }

            // Reads all fields.
            foreach (KeyValuePair<string, PropertyInfo> kv in readableFields)
            {
                object value = GetFiledValue(kv.Key);
                if (value != null)
                {
                    ret.Add(kv.Key, value);
                }
            }

            return ret;
        }

        /// <summary>
        /// Called when a property setting request is received.
        /// To add extra processing when writing properties, you can override this method.
        /// </summary>
        /// <param name="properties">Indicates the desired properties.</param>
        /// <returns>Returns the operation result.</returns>
        public IotResult OnWrite(Dictionary<string, object> properties)
        {
            List<string> changedProps = new List<string>();

            foreach (KeyValuePair<string, object> kv in properties)
            {
                try
                {
                    if (!writeableFields.ContainsKey(kv.Key))
                    {
                        return new IotResult(-1, "property " + kv.Key + " is read only");
                    }

                    string propertyName = writeableFields[kv.Key].Name;

                    string setter = "set_" + propertyName;

                    if (!commands.ContainsKey(setter))
                    {
                        Log.Error("the setter not found in commands:" + setter);
                        continue;
                    }

                    // A reflection call method.
                    MethodInfo nonstaticMethod = commands[setter];

                    // Sets a value.
                    if (SetFiledValue(nonstaticMethod, kv.Value) != 1)
                    {
                        Log.Error("write property fail:" + propertyName);
                        continue;
                    }

                    Log.Info("write property ok:" + propertyName);
                    changedProps.Add(kv.Key);
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);

                    return new IotResult(-1, e.Message);
                }
            }

            // Reports changed properties.
            if (changedProps.Count > 0)
            {
                FirePropertiesChanged(changedProps.ToArray());
            }

            return IotResult.SUCCESS;
        }

        /// <summary>
        /// Reports a property change.
        /// </summary>
        /// <param name="properties">Indicates the properties changed. If it is set to NULL, changes of all readable properties are reported.</param>
        public void FirePropertiesChanged(params string[] properties)
        {
            iotDevice.FirePropertiesChanged(ServiceId, properties);
        }
        
        public void EnableAutoReport(int reportInterval)
        {
            if (timer != null)
            {
                Log.Error("timer is already enabled");

                return;
            }
            else
            {
                FirePropertiesChanged();
                timer = new Timer();

                // Set the interval. The default value is 10 seconds.
                timer.Interval = reportInterval;

                // Allow the timer.
                timer.Enabled = true;

                // Define a callback.
                timer.Elapsed += new ElapsedEventHandler(Timer_Event);

                // Define multiple loops.
                timer.AutoReset = true;
            }
        }

        /// <summary>
        /// Disables automatic, periodic property reporting. You can use firePropertiesChanged to trigger property reporting.
        /// </summary>
        public void DisableAutoReport()
        {
            if (timer != null)
            {
                timer.Close();
                timer = null;
            }
        }

        private void Timer_Event(object sender, ElapsedEventArgs e)
        {
            FirePropertiesChanged();
        }

        private int SetFiledValue(MethodInfo methodInfo, object value)
        {
            try
            {
                ParameterInfo[] paramsInfo = methodInfo.GetParameters(); // Obtains parameters with the specified method.
                object[] objValue = new object[paramsInfo.Length];
                for (int i = 0; i < paramsInfo.Length; i++)
                {
                    Type tType = paramsInfo[i].ParameterType;

                    // If it is a value type or a string.
                    if (tType.Equals(typeof(string)) || (!tType.IsInterface && !tType.IsClass))
                    {
                        // change the parameter type.
                        objValue[i] = Convert.ChangeType(value, tType);
                    }
                }

                methodInfo.Invoke(deviceService, objValue);

                return 1;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);

                return -1;
            }
        }

        private object GetFiledValue(string propertyName)
        {
            try
            {
                string getter = "get_" + readableFields[propertyName].Name;
                if (!commands.ContainsKey(getter))
                {
                    Log.Error("the getter not found in commands" + getter);

                    return null;
                }

                MethodInfo nonstaticMethod = commands[getter];

                // Class instance required for non-static method calls
                object value = nonstaticMethod.Invoke(deviceService, null);

                return value;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            return null;
        }
    }
}
