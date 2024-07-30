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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using NLog;

namespace IoT.SDK.Device.Service.Anomaly
{
    public class DefaultSecurityInfoGetListener : SecurityInfoGetListener
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        private LinuxCpuUsageCalculator linuxCpuUsageCalculator;

        private class LinuxCpuUsageCalculator
        {
            private long TotalTime { get; set; }

            private long IdleTime { get; set; }

            private void RefreshCpuInfoLinux()
            {
                var cpuUsageLines = File.ReadAllLines("/proc/stat").FirstOrDefault()
                    ?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var convertedValues = cpuUsageLines.Skip(1).Select(int.Parse).ToList();
                TotalTime = convertedValues.Sum();
                // idleTime and ioWaitTime 
                IdleTime = convertedValues[4] + convertedValues[5];
            }

            public int GetCpuLoad()
            {
                var prevIdleTime = IdleTime;
                var prevTotalTime = TotalTime;
                // calculate the CPU usage since the last check
                RefreshCpuInfoLinux();
                var cpuUsage = (1 - (IdleTime - prevIdleTime) / (prevTotalTime - TotalTime)) * 100;
                return (int)cpuUsage;
            }

            public LinuxCpuUsageCalculator()
            {
                RefreshCpuInfoLinux();
            }
        }

        public void OnStart()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                linuxCpuUsageCalculator = new LinuxCpuUsageCalculator();
            }
        }

        public void OnStop()
        {
        }


        private static long GetMemoryKbInfoLinux(string name)
        {
            var memInfoLines = File.ReadAllLines("/proc/meminfo").ToList();
            var memoryLine = memInfoLines.FirstOrDefault(line => line.StartsWith($"{name}:"));

            if (memoryLine == null) return 0;
            var parts = memoryLine.Split();
            if (parts.Length < 2) return 0;
            var value = int.Parse(parts[1]);
            return value;
        }

        private static long GetMemoryKbInfoWindows(string keyName)
        {
            var info = new ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = $"OS get {keyName} /Value",
                RedirectStandardOutput = true
            };


            long memorySize = 0;

            using var process = Process.Start(info);
            if (process == null) return memorySize;
            var output = process.StandardOutput.ReadToEnd();
            var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) continue;

                var name = parts[0].Trim();
                var value = parts[1].Trim();

                if (name != keyName) continue;

                memorySize = long.Parse(value);
                break;
            }

            return memorySize;
        }

        public long OnGetMemoryTotalKb()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? GetMemoryKbInfoWindows("TotalVisibleMemorySize")
                : GetMemoryKbInfoLinux("MemTotal");
        }

        public long OnGetMemoryUsedKb()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? GetMemoryKbInfoWindows("TotalVisibleMemorySize") - GetMemoryKbInfoWindows("FreePhysicalMemory")
                : GetMemoryKbInfoLinux("MemTotal") - GetMemoryKbInfoLinux("MemFree");
        }

        public IEnumerable<int> OnGetUsedPort()
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpEndPoints = properties.GetActiveTcpListeners();
            var udpEndPoints = properties.GetActiveUdpListeners();
            return tcpEndPoints.Select(t => t.Port).Concat(udpEndPoints.Select(t => t.Port));
        }


        private static int GetCpuPercentageWindows()
        {
            var info = new ProcessStartInfo
            {
                FileName = "typeperf",
                Arguments = "\"\\Processor(_Total)\\% Processor Time\" -sc 1",
                RedirectStandardOutput = true
            };


            using var process = Process.Start(info);
            if (process == null) return 0;
            var output = process.StandardOutput.ReadToEnd();
            var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var values = lines[1].Split(',');
            return (int)double.Parse(values[1].Trim('\"'));
        }

        public int OnGetCpuPercentage()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? GetCpuPercentageWindows()
                : linuxCpuUsageCalculator.GetCpuLoad();
        }

        public long OnGetDiskTotalKb()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            long totalSize = 0;

            foreach (DriveInfo d in allDrives)
            {
                if (!d.IsReady) continue;
                totalSize += d.TotalSize / 1024;
                LOG.Debug("Drive {0} total size: {1}", d.Name, d.TotalSize / 1024);
            }

            return totalSize;
        }

        public long OnGetDiskUsedKb()
        {
            var allDrives = DriveInfo.GetDrives();

            long usedSpace = 0;

            foreach (var d in allDrives)
            {
                if (!d.IsReady) continue;
                var currentUsedSpace = (d.TotalSize - d.AvailableFreeSpace) / 1024;
                usedSpace += currentUsedSpace;
                LOG.Debug("Drive {0} used size: {1}", d.Name, currentUsedSpace);
            }

            return usedSpace;
        }

        public int OnGetBatteryPercentage()
        {
            return new Random().Next() % 100;
        }

        public virtual bool OnGetLocalLoginInfo()
        {
            throw new NotImplementedException();
        }

        public virtual bool OnGetFileTamperInfo()
        {
            throw new NotImplementedException();
        }

        public virtual bool OnGetBruteForceLoginInfo()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPAddress> OnGetMaliciousIp()
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var connections = properties.GetActiveTcpConnections();
            return connections.Select(v => v.RemoteEndPoint.Address).Distinct();
        }
    }
}