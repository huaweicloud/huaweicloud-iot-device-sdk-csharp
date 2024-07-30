/*
 * Copyright (c) 2023-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.IO;
using System.Threading;
using IoT.SDK.Device.Gateway;
using IoT.SDK.Device.Gateway.Requests;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.Device.Demo.Gateway
{
    /// <summary>
    /// Saves sub device details to a JSON file. You can override this method.
    /// </summary>
    public class SubDevicesFilePersistence : SubDevicesPersistence
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly Mutex mutex = new Mutex(false);

        private SubDevInfo subDevInfoCache;
        private readonly string subDeviceInfoPath;

        private SubDevInfo ReadDeviceInfo()
        {
            try
            {
                using var streamReader = new StreamReader(subDeviceInfoPath);
                return JsonUtil.ConvertJsonStringToObject<SubDevInfo>(streamReader.ReadToEnd());
            }
            catch (DirectoryNotFoundException)
            {
                //ignore
                LOG.Info("create new sub device info file");
            }
            catch (Exception e)
            {
                LOG.Error(e, "read json file fail");
            }

            return new SubDevInfo();
        }

        void StoreDeviceInfo(SubDevInfo info)
        {
            var file = new FileInfo(subDeviceInfoPath);
            file.Directory?.Create();
            File.WriteAllText(file.FullName, JsonUtil.ConvertObjectToJsonString(info));
        }

        public SubDevicesFilePersistence(string infoPath)
        {
            subDeviceInfoPath = infoPath;
            subDevInfoCache = ReadDeviceInfo();
            LOG.Info("subDevInfo: {}", subDevInfoCache);
        }

        public DeviceInfo GetSubDevice(string nodeId)
        {
            mutex.WaitOne();
            try
            {
                return !subDevInfoCache.subdevices.ContainsKey(nodeId) ? null : subDevInfoCache.subdevices[nodeId];
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public int AddSubDevices(SubDevicesInfo subDevicesInfo)
        {
            return UpdateSubDeviceToFile((info, _) =>
            {
                foreach (DeviceInfo dev in subDevicesInfo.devices)
                {
                    info.subdevices[dev.nodeId] = dev;
                    LOG.Info("add subdev: " + dev.nodeId);
                }
            }, subDevicesInfo, "add");
        }

        public int DeleteSubDevices(SubDevicesInfo subDevicesInfo)
        {
            return UpdateSubDeviceToFile((info, _) =>
            {
                foreach (DeviceInfo dev in subDevicesInfo.devices)
                {
                    info.subdevices.Remove(dev.nodeId);
                    LOG.Info("remove sub device:" + dev.nodeId);
                }
            }, subDevicesInfo, "delete");
        }

        public long GetVersion()
        {
            mutex.WaitOne();
            try
            {
                return subDevInfoCache.version;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        private int UpdateSubDeviceToFile(Action<SubDevInfo, SubDevicesInfo> updater,
            SubDevicesInfo subDevicesInfo, string operationName)
        {
            mutex.WaitOne();
            try
            {
                if (subDevicesInfo.version > 0 && subDevicesInfo.version <= subDevInfoCache.version)
                {
                    LOG.Info("version too low:{} ", subDevicesInfo.version);
                    return -1;
                }

                var nextSubDevInfoCache = ReadDeviceInfo();
                updater(nextSubDevInfoCache, subDevicesInfo);
                nextSubDevInfoCache.version = subDevicesInfo.version;
                LOG.Info("version update to {}", nextSubDevInfoCache.version);
                subDevInfoCache = nextSubDevInfoCache;
                StoreDeviceInfo(subDevInfoCache);
            }
            catch (Exception ex)
            {
                LOG.Error(ex, "{:l} sub device fail in json file", operationName);
                return -1;
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            return 0;
        }
    }
}