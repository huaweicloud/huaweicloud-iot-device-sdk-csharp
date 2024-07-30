/*
 * Copyright (c) 2024-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace IoT.SDK.Device.Service.Anomaly
{
    public class SecurityDetectionService : AbstractService
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public SecurityInfoGetListener SecurityInfoGetListener { get; set; } = new DefaultSecurityInfoGetListener();
        public TimeSpan ReportPeriod { get; set; } = TimeSpan.FromMinutes(5);

        private class SubDetectionInfo
        {
            public long Check { get; set; }
            public string ReportType { get; set; }
            public Func<JToken> DataGetter { get; set; }

            public SubDetectionInfo(long check, string reportType, Func<JToken> dataGetter)
            {
                Check = check;
                ReportType = reportType;
                DataGetter = dataGetter;
            }
        }

        private readonly Dictionary<string, SubDetectionInfo> detectionCheck;
        private Timer securityInfoReportTimer;

        private long MemoryThreshold { get; set; } = 90;
        private long CpuUsageThreshold { get; set; } = 90;
        private long DiskSpaceThreshold { get; set; } = 90;
        private long BatteryPercentageThreshold { get; set; } = 30;

        public void AfterConnected()
        {
            GetShadow();
        }

        public override IotResult OnWrite(Dictionary<string, object> properties)
        {
            if (properties.TryGetValue("memoryThreshold", out var memoryThresholdVal))
            {
                MemoryThreshold = (long)memoryThresholdVal;
            }

            if (properties.TryGetValue("cpuUsageThreshold", out var cpuUsageThresholdVal))
            {
                CpuUsageThreshold = (long)cpuUsageThresholdVal;
            }

            if (properties.TryGetValue("diskSpaceThreshold", out var diskSpaceThresholdVal))
            {
                DiskSpaceThreshold = (long)diskSpaceThresholdVal;
            }

            if (properties.TryGetValue("batteryPercentageThreshold", out var batteryPercentageThresholdVal))
            {
                BatteryPercentageThreshold = (long)batteryPercentageThresholdVal;
            }


            foreach (var (checkName, value) in detectionCheck)
            {
                if (properties.TryGetValue(checkName, out var newCheckValue))
                {
                    detectionCheck[checkName].Check = (long)newCheckValue;
                }
            }


            var newNeedToReport = detectionCheck.Values.Aggregate(0L,
                (current, detectionInfo) => current + detectionInfo.Check);

            if (newNeedToReport == 0)
            {
                if (securityInfoReportTimer != null)
                {
                    securityInfoReportTimer?.Dispose();
                    securityInfoReportTimer = null;
                    SecurityInfoGetListener?.OnStop();
                    LOG.Info("security detection exited");
                }
            }
            else
            {
                if (securityInfoReportTimer == null)
                {
                    LOG.Info("security detection started");
                    SecurityInfoGetListener?.OnStart();
                    securityInfoReportTimer =
                        new Timer(_ => GetAndReportSecurityDetect(), null, TimeSpan.Zero, ReportPeriod);
                }
            }


            var listProperties = new List<ServiceProperty>
            {
                new ServiceProperty
                {
                    serviceId = GetServiceId(),
                    properties = properties,
                    eventTime = IotUtil.GetEventTime()
                }
            };

            iotDevice.GetClient().ReportProperties(listProperties);
            return IotResult.SUCCESS;
        }


        private void GetAndReportSecurityDetect()
        {
            foreach (var subDetectionInfo in detectionCheck.Values)
            {
                if (subDetectionInfo.Check == 0) continue;
                try
                {
                    var content = subDetectionInfo.DataGetter();
                    var e = new DeviceEvent
                    {
                        serviceId = "$log",
                        eventType = "security_log_report",
                        eventTime = IotUtil.GetEventTime(),
                        paras = new Dictionary<string, object>
                        {
                            { "timestamp", IotUtil.GetEventTime() },
                            { "type", subDetectionInfo.ReportType },
                            { "content", content }
                        }
                    };
                    iotDevice.GetClient().ReportEvent(e);
                }
                catch (NotImplementedException)
                {
                    //ignore
                }
                catch (Exception exception)
                {
                    LOG.Error(exception, "get {} failed", subDetectionInfo.ReportType);
                }
            }
        }

        public SecurityDetectionService()
        {
            detectionCheck = new Dictionary<string, SubDetectionInfo>
            {
                { "memoryCheck", new SubDetectionInfo(0, "MEMORY_REPORT", GetMemoryCheck) },
                { "portCheck", new SubDetectionInfo(0, "PORT_REPORT", GetPortCheck) },
                { "cpuUsageCheck", new SubDetectionInfo(0, "CPU_USAGE_REPORT", GetCpuUsageCheck) },
                { "diskSpaceCheck", new SubDetectionInfo(0, "DISK_SPACE_REPORT", GetDiskSpaceCheck) },
                { "batteryPercentageCheck", new SubDetectionInfo(0, "BATTERY_REPORT", GetBatteryPercentageCheck) },
                { "loginLocalCheck", new SubDetectionInfo(0, "LOGIN_LOCAL_REPORT", GetLoginLocalCheck) },
                { "fileTamperCheck", new SubDetectionInfo(0, "FILE_TAMPER_REPORT", GetFileTamperCheck) },
                {
                    "loginBruteForceCheck", new SubDetectionInfo(0, "LOGIN_BRUTE_FORCE_REPORT", GetLoginBruteForceCheck)
                },
                { "maliciousIPCheck", new SubDetectionInfo(0, "IP_REPORT", GetMaliciousIpCheck) }
            };

            SetDeviceService(this);
        }

        public override string GetServiceId()
        {
            return "$security_detection_config";
        }


        private JToken GetMemoryCheck()
        {
            var total = SecurityInfoGetListener?.OnGetMemoryTotalKb();
            var used = SecurityInfoGetListener?.OnGetMemoryUsedKb();
            if (used == null || total == null)
            {
                return null;
            }

            int isLeakAlarm = 0;
            var usagePercentage = (double)used / total * 100;
            if (usagePercentage >= MemoryThreshold)
            {
                isLeakAlarm = 1;
                LOG.Debug("the memory usage exceeds threshold, used percentage = {}%", usagePercentage);
            }
            else
            {
                LOG.Debug("the memory is enough, used percentage = {}%", usagePercentage);
            }

            return new JObject
            {
                { "used", used },
                { "total", total },
                { "leak_alarm", isLeakAlarm }
            };
        }

        private JToken GetPortCheck()
        {
            var usedPort = SecurityInfoGetListener?.OnGetUsedPort().Distinct();
            if (usedPort == null)
            {
                return null;
            }

            return new JObject
            {
                { "used", new JArray(usedPort) }
            };
        }

        private JToken GetCpuUsageCheck()
        {
            var cpuUsage = SecurityInfoGetListener?.OnGetCpuPercentage();
            if (cpuUsage == null)
            {
                return null;
            }

            var cpuAlarm = 0;
            if (cpuUsage >= CpuUsageThreshold)
            {
                cpuAlarm = 1;
                LOG.Debug("the cpu usage exceeds threshold, cpuUsage = {}", cpuUsage);
            }
            else
            {
                LOG.Debug("the cpu is enough, cpuUsage = {}", cpuUsage);
            }

            return new JObject
            {
                { "cpu_usage", cpuUsage },
                { "cpu_usage_alarm", cpuAlarm }
            };
        }

        private JToken GetDiskSpaceCheck()
        {
            var diskSpaceTotal = SecurityInfoGetListener?.OnGetDiskTotalKb();
            var diskSpaceUsed = SecurityInfoGetListener?.OnGetDiskUsedKb();
            if (diskSpaceTotal == null || diskSpaceUsed == null)
            {
                return null;
            }

            int diskAlarm = 0;
            var usagePercentage = (double)diskSpaceUsed / diskSpaceTotal * 100;
            if (usagePercentage >= DiskSpaceThreshold)
            {
                diskAlarm = 1;
                LOG.Debug("the disk space used exceeds threshold");
            }
            else
            {
                LOG.Debug("the disk space is enough, used {}%", usagePercentage);
            }

            return new JObject
            {
                { "disk_space_used", diskSpaceUsed },
                { "disk_space_total", diskSpaceTotal },
                { "disk_space_alarm", diskAlarm },
            };
        }

        private JToken GetBatteryPercentageCheck()
        {
            var batteryPct = SecurityInfoGetListener?.OnGetBatteryPercentage();
            if (batteryPct == null)
            {
                return null;
            }

            var batteryAlarm = 0;
            if (batteryPct <= BatteryPercentageThreshold)
            {
                batteryAlarm = 1;
                LOG.Debug("the battery is low ");
            }
            else
            {
                LOG.Debug("the battery is enough, batteryPct = {}", batteryPct);
            }

            return new JObject
            {
                { "battery_percentage", batteryPct },
                { "battery_percentage_alarm", batteryAlarm },
            };
        }

        private JToken GetLoginLocalCheck()
        {
            var localLoginFlag = SecurityInfoGetListener?.OnGetLocalLoginInfo();
            if (localLoginFlag == null)
            {
                return null;
            }

            return new JObject
            {
                { "login_local_alarm", localLoginFlag }
            };
        }

        private JToken GetFileTamperCheck()
        {
            var fileTamperFlag = SecurityInfoGetListener?.OnGetFileTamperInfo();
            if (fileTamperFlag == null)
            {
                return null;
            }

            return new JObject
            {
                { "file_tamper_alarm", fileTamperFlag }
            };
        }

        private JToken GetLoginBruteForceCheck()
        {
            var bruteForceLoginFlag = SecurityInfoGetListener?.OnGetBruteForceLoginInfo();
            if (bruteForceLoginFlag == null)
            {
                return null;
            }

            return new JObject
            {
                { "login_brute_force_alarm", bruteForceLoginFlag }
            };
        }

        private JToken GetMaliciousIpCheck()
        {
            var maliciousIp = SecurityInfoGetListener?.OnGetMaliciousIp().Distinct();
            if (maliciousIp == null)
            {
                return null;
            }

            ;

            return new JObject
            {
                { "used_ips", new JArray(maliciousIp.Select(v => v.ToString())) }
            };
        }
    }
}