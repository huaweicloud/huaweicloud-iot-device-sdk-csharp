/*Copyright (c) <2020>, <Huawei Technologies Co., Ltd>
 * All rights reserved.
 * &Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice, this list of
 * conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list
 * of conditions and the following disclaimer in the documentation and/or other materials
 * provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used
 * to endorse or promote products derived from this software without specific prior written permission.
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 *
 * */

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
    /// 抽象服务类，提供了属性自动读写和命令调用能力，用户可以继承此类，根据物模型定义自己的服务
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

                // 反射调用方法
                MethodInfo nonstaticMethod = deviceServiceType.GetMethod(methodInfo.Name);

                // 非静态方法调用需要类实例
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
        /// 事件处理。收到平台下发的事件时此接口被自动调用。默认为空实现
        /// </summary>
        /// <param name="deviceEvent"></param>
        public virtual void OnEvent(DeviceEvent deviceEvent)
        {
            Log.Info("onEvent no op");
        }

        public void SetDeviceService<T>(T deviceService)
        {
            this.deviceService = deviceService;
            deviceServiceType = typeof(T); // 反射对象
            var deviceServiceProperties = deviceServiceType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance); // 获取对象属性

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
        /// 读属性回调
        /// </summary>
        /// <param name="properties">指定读取的字段名，不指定则读取全部可读字段</param>
        /// <returns>属性值</returns>
        public Dictionary<string, object> OnRead(params string[] properties)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();

            // 读取指定的字段
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

            // 读取全部字段
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
        /// 写属性。收到平台下发的写属性操作时此接口被自动调用。
        /// 如果用户希望在写属性时增加额外处理，可以重写此接口
        /// </summary>
        /// <param name="properties">平台期望属性的值</param>
        /// <returns>操作结果</returns>
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

                    // 反射调用方法
                    MethodInfo nonstaticMethod = commands[setter];

                    // 调用方法设置值
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

            // 上报变化的属性
            if (changedProps.Count > 0)
            {
                FirePropertiesChanged(changedProps.ToArray());
            }

            return IotResult.SUCCESS;
        }
        
        /// <summary>
        /// 通知服务属性变化
        /// </summary>
        /// <param name="properties">变化的属性，不指定默认读取全部可读属性</param>
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

                // 循环间隔时间(10秒钟)
                timer.Interval = reportInterval;

                // 允许Timer执行
                timer.Enabled = true;

                // 定义回调
                timer.Elapsed += new ElapsedEventHandler(Timer_Event);

                // 定义多次循环
                timer.AutoReset = true;
            }
        }

        /// <summary>
        /// 关闭自动周期上报，您可以通过FirePropertiesChanged触发上报
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
                ParameterInfo[] paramsInfo = methodInfo.GetParameters(); // 得到指定方法的参数列表
                object[] objValue = new object[paramsInfo.Length];
                for (int i = 0; i < paramsInfo.Length; i++)
                {
                    Type tType = paramsInfo[i].ParameterType;

                    // 如果它是值类型,或者string
                    if (tType.Equals(typeof(string)) || (!tType.IsInterface && !tType.IsClass))
                    {
                        // 改变参数类型
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

                // 非静态方法调用需要类实例
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
