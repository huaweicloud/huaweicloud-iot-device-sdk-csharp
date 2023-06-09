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
