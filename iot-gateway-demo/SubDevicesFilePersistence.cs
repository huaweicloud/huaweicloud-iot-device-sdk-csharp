/*
 * Copyright (c) 2023-2023 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.IO;
using System.Threading;
using IoT.SDK.Device.Config;
using IoT.SDK.Device.Gateway;
using IoT.SDK.Device.Gateway.Requests;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.Gateway.Demo
{
    /// <summary>
    /// Saves sub device details to a JSON file. You can override this method.
    /// </summary>
    public class SubDevicesFilePersistence : SubDevicesPersistence
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private Mutex mutex = new Mutex(false);

        private SubDevInfo subDevInfoCache;
        
        public SubDevicesFilePersistence()
        {
            string content = IotUtil.ReadJsonFile(CommonFilePath.SUB_DEVICES_PATH);
            
            this.subDevInfoCache = JsonUtil.ConvertJsonStringToObject<SubDevInfo>(content);

            Log.Info("subDevInfo:" + subDevInfoCache.ToString());
        }

        public DeviceInfo GetSubDevice(string nodeId)
        {
            if (!subDevInfoCache.subdevices.ContainsKey(nodeId))
            {
                return null;
            }

            return subDevInfoCache.subdevices[nodeId];
        }

        public int AddSubDevices(SubDevicesInfo subDevicesInfo)
        {
            mutex.WaitOne();
            try
            {
                if (subDevicesInfo.version > 0 && subDevicesInfo.version <= subDevInfoCache.version)
                {
                    Log.Info("version too low: " + subDevicesInfo.version);

                    return -1;
                }

                if (AddSubDeviceToFile(subDevicesInfo) != 0)
                {
                    Log.Info("write file fail ");

                    return -1;
                }

                if (subDevInfoCache.subdevices == null)
                {
                    subDevInfoCache.subdevices = new Dictionary<string, DeviceInfo>();
                }
                
                foreach (DeviceInfo dev in subDevicesInfo.devices)
                {
                    subDevInfoCache.subdevices.Add(dev.nodeId, dev);
                    Log.Info("add subdev: " + dev.nodeId);
                }

                subDevInfoCache.version = subDevicesInfo.version;
                Log.Info("version update to " + subDevInfoCache.version);
            }
            finally
            {
                // Releases a mutex.
                mutex.ReleaseMutex();
            }

            return 0;
        }

        public int DeleteSubDevices(SubDevicesInfo subDevicesInfo)
        {
            mutex.WaitOne();
            try
            {
                if (subDevicesInfo.version > 0 && subDevicesInfo.version <= subDevInfoCache.version)
                {
                    Log.Info("version too low: " + subDevicesInfo.version);

                    return -1;
                }

                if (subDevInfoCache.subdevices == null)
                {
                    return -1;
                }

                if (RmvSubDeviceToFile(subDevicesInfo) != 0)
                {
                    Log.Info("remove from file fail ");

                    return -1;
                }

                foreach (DeviceInfo dev in subDevicesInfo.devices)
                {
                    subDevInfoCache.subdevices.Remove(dev.nodeId);
                    Log.Info("rmv subdev :" + dev.nodeId);
                }

                subDevInfoCache.version = subDevicesInfo.version;
                Log.Info("local version update to " + subDevicesInfo.version);
            }
            finally
            {
                // Releases a mutex.
                mutex.ReleaseMutex();
            }
            
            return 0;
        }

        public long GetVersion()
        {
            return subDevInfoCache.version;
        }

        private int AddSubDeviceToFile(SubDevicesInfo subDevicesInfo)
        {
            try
            {
                string content = IotUtil.ReadJsonFile(CommonFilePath.SUB_DEVICES_PATH);

                SubDevInfo subDevInfo = JsonUtil.ConvertJsonStringToObject<SubDevInfo>(content);

                if (subDevInfo.subdevices == null)
                {
                    subDevInfo.subdevices = new Dictionary<string, DeviceInfo>();
                }

                foreach (DeviceInfo dev in subDevicesInfo.devices)
                {
                    subDevInfo.subdevices.Add(dev.nodeId, dev);
                    subDevInfo.version = subDevicesInfo.version;
                }
                
                File.WriteAllText(CommonFilePath.SUB_DEVICES_PATH, JsonUtil.ConvertObjectToJsonString(subDevInfo));
            }
            catch (Exception ex)
            {
                Log.Error("add sub device fail in json file");

                return -1;
            }

            return 0;
        }

        private int RmvSubDeviceToFile(SubDevicesInfo subDevicesInfo)
        {
            try
            {
                string content = IotUtil.ReadJsonFile(CommonFilePath.SUB_DEVICES_PATH);

                SubDevInfo subDevInfo = JsonUtil.ConvertJsonStringToObject<SubDevInfo>(content);

                if (subDevInfo.subdevices == null)
                {
                    return 0;
                }

                foreach (DeviceInfo dev in subDevicesInfo.devices)
                {
                    subDevInfo.subdevices.Remove(dev.nodeId);
                    subDevInfo.version = subDevicesInfo.version;
                }

                File.WriteAllText(CommonFilePath.SUB_DEVICES_PATH, JsonUtil.ConvertObjectToJsonString(subDevInfo));
            }
            catch (Exception ex)
            {
                Log.Error("remove sub device fail in json file");

                return -1;
            }

            return 0;
        }
    }
}
